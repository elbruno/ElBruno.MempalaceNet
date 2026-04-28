using System.Collections.Concurrent;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;

namespace MemPalace.Core.Backends.InMemory;

internal sealed class InMemoryCollection : ICollection
{
    private readonly ConcurrentDictionary<string, StoredRecord> _records = new();

    public string Name { get; }
    public int Dimensions { get; }
    public string EmbedderIdentity { get; }

    public InMemoryCollection(string name, int dimensions, string embedderIdentity)
    {
        Name = name;
        Dimensions = dimensions;
        EmbedderIdentity = embedderIdentity;
    }

    public ValueTask AddAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            ValidateDimensions(record.Embedding);
            
            if (!_records.TryAdd(record.Id, new StoredRecord(record.Document, record.Metadata, record.Embedding)))
                throw new InvalidOperationException($"Record with ID '{record.Id}' already exists.");
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask UpsertAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            ValidateDimensions(record.Embedding);
            _records[record.Id] = new StoredRecord(record.Document, record.Metadata, record.Embedding);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<GetResult> GetAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        int? limit = null,
        int offset = 0,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default)
    {
        var filtered = FilterRecords(ids, where);
        var paginated = filtered.Skip(offset).Take(limit ?? int.MaxValue).ToList();

        var resultIds = new List<string>();
        var docs = new List<string>();
        var metas = new List<IReadOnlyDictionary<string, object?>>();
        var embeddings = include.HasFlag(IncludeFields.Embeddings) ? new List<ReadOnlyMemory<float>>() : null;

        foreach (var (id, record) in paginated)
        {
            resultIds.Add(id);
            docs.Add(include.HasFlag(IncludeFields.Documents) ? record.Document : "");
            metas.Add(include.HasFlag(IncludeFields.Metadatas) ? record.Metadata : new Dictionary<string, object?>());
            embeddings?.Add(record.Embedding);
        }

        return ValueTask.FromResult(new GetResult(resultIds, docs, metas, embeddings));
    }

    public ValueTask<QueryResult> QueryAsync(
        IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings,
        int nResults = 10,
        WhereClause? where = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
        CancellationToken ct = default)
    {
        var filtered = FilterRecords(null, where).ToList();

        var allIds = new List<IReadOnlyList<string>>();
        var allDocs = new List<IReadOnlyList<string>>();
        var allMetas = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        var allDists = new List<IReadOnlyList<float>>();
        var allEmbeds = include.HasFlag(IncludeFields.Embeddings) ? new List<IReadOnlyList<ReadOnlyMemory<float>>>() : null;

        foreach (var queryEmbed in queryEmbeddings)
        {
            var scored = filtered
                .Select(kv => (id: kv.Key, record: kv.Value, distance: CosineSimilarity(queryEmbed, kv.Value.Embedding)))
                .OrderBy(x => x.distance)
                .Take(nResults)
                .ToList();

            var ids = new List<string>();
            var docs = new List<string>();
            var metas = new List<IReadOnlyDictionary<string, object?>>();
            var dists = new List<float>();
            var embeds = include.HasFlag(IncludeFields.Embeddings) ? new List<ReadOnlyMemory<float>>() : null;

            foreach (var (id, record, distance) in scored)
            {
                ids.Add(id);
                docs.Add(include.HasFlag(IncludeFields.Documents) ? record.Document : "");
                metas.Add(include.HasFlag(IncludeFields.Metadatas) ? record.Metadata : new Dictionary<string, object?>());
                dists.Add(include.HasFlag(IncludeFields.Distances) ? distance : 0f);
                embeds?.Add(record.Embedding);
            }

            allIds.Add(ids);
            allDocs.Add(docs);
            allMetas.Add(metas);
            allDists.Add(dists);
            allEmbeds?.Add(embeds!);
        }

        return ValueTask.FromResult(new QueryResult(allIds, allDocs, allMetas, allDists, allEmbeds));
    }

    public ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        return ValueTask.FromResult((long)_records.Count);
    }

    public ValueTask DeleteAsync(IReadOnlyList<string>? ids = null, WhereClause? where = null, CancellationToken ct = default)
    {
        var toDelete = FilterRecords(ids, where).Select(kv => kv.Key).ToList();
        foreach (var id in toDelete)
            _records.TryRemove(id, out _);

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<EmbeddedRecord>> WakeUpAsync(
        int limit = 20,
        WhereClause? where = null,
        DateTime? sinceDate = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default)
    {
        var filtered = FilterRecords(null, where);

        // Apply date filter if specified
        if (sinceDate.HasValue)
        {
            filtered = filtered.Where(kv =>
            {
                if (kv.Value.Metadata.TryGetValue("timestamp", out var ts))
                {
                    var timestamp = ParseTimestamp(ts);
                    return timestamp >= sinceDate.Value;
                }
                return false;
            });
        }

        // Sort by timestamp descending (most recent first) and take limit
        var sorted = filtered
            .Select(kv => new
            {
                Id = kv.Key,
                Record = kv.Value,
                Timestamp = kv.Value.Metadata.TryGetValue("timestamp", out var ts)
                    ? ParseTimestamp(ts)
                    : DateTime.MinValue
            })
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToList();

        var results = sorted
            .Select(x => new EmbeddedRecord(
                x.Id,
                x.Record.Document,
                x.Record.Metadata,
                include.HasFlag(IncludeFields.Embeddings) ? x.Record.Embedding : ReadOnlyMemory<float>.Empty))
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<EmbeddedRecord>>(results);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static DateTime ParseTimestamp(object? value)
    {
        if (value == null) return DateTime.MinValue;
        
        if (value is DateTime dt) return dt;
        if (value is DateTimeOffset dto) return dto.UtcDateTime;
        if (value is string str && DateTime.TryParse(str, out var parsed)) return parsed;
        if (value is long ticks) return new DateTime(ticks, DateTimeKind.Utc);
        
        return DateTime.MinValue;
    }

    private void ValidateDimensions(ReadOnlyMemory<float> embedding)
    {
        if (embedding.Length != Dimensions)
            throw new DimensionMismatchException($"Expected {Dimensions} dimensions, but got {embedding.Length}.");
    }

    private IEnumerable<KeyValuePair<string, StoredRecord>> FilterRecords(IReadOnlyList<string>? ids, WhereClause? where)
    {
        var records = _records.AsEnumerable();

        if (ids != null && ids.Count > 0)
        {
            var idSet = new HashSet<string>(ids);
            records = records.Where(kv => idSet.Contains(kv.Key));
        }

        if (where != null)
            records = records.Where(kv => EvaluateWhere(where, kv.Value.Metadata));

        return records;
    }

    private bool EvaluateWhere(WhereClause clause, IReadOnlyDictionary<string, object?> metadata)
    {
        return clause switch
        {
            Eq eq => metadata.TryGetValue(eq.Field, out var val) && Equals(val, eq.Value),
            NotEq ne => !metadata.TryGetValue(ne.Field, out var val) || !Equals(val, ne.Value),
            Gt gt => CompareValues(metadata, gt.Field, gt.Value, (a, b) => a.CompareTo(b) > 0),
            Gte gte => CompareValues(metadata, gte.Field, gte.Value, (a, b) => a.CompareTo(b) >= 0),
            Lt lt => CompareValues(metadata, lt.Field, lt.Value, (a, b) => a.CompareTo(b) < 0),
            Lte lte => CompareValues(metadata, lte.Field, lte.Value, (a, b) => a.CompareTo(b) <= 0),
            In inClause => metadata.TryGetValue(inClause.Field, out var val) && inClause.Values.Contains(val),
            NotIn notIn => !metadata.TryGetValue(notIn.Field, out var val) || !notIn.Values.Contains(val),
            And and => and.Clauses.All(c => EvaluateWhere(c, metadata)),
            Or or => or.Clauses.Any(c => EvaluateWhere(c, metadata)),
            _ => throw new UnsupportedFilterException($"Unsupported filter clause: {clause.GetType().Name}")
        };
    }

    private bool CompareValues(IReadOnlyDictionary<string, object?> metadata, string field, object? value, Func<IComparable, IComparable, bool> comparison)
    {
        if (!metadata.TryGetValue(field, out var metaVal))
            return false;

        if (metaVal is IComparable metaComp && value is IComparable valComp)
        {
            try
            {
                return comparison(metaComp, valComp);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;

        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < aSpan.Length; i++)
        {
            dot += aSpan[i] * bSpan[i];
            magA += aSpan[i] * aSpan[i];
            magB += bSpan[i] * bSpan[i];
        }

        var denominator = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        if (denominator == 0f)
            return float.MaxValue;

        // Convert similarity to distance (lower is better)
        return 1f - (dot / denominator);
    }

    private sealed record StoredRecord(
        string Document,
        IReadOnlyDictionary<string, object?> Metadata,
        ReadOnlyMemory<float> Embedding);
}
