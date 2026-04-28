using System.Text;
using System.Text.Json;
using MemPalace.Core.Backends;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;
using Microsoft.Data.Sqlite;

namespace MemPalace.Backends.Sqlite;

/// <summary>
/// SQLite-based collection implementation. Stores embeddings as BLOBs and performs brute-force cosine similarity search.
/// </summary>
public sealed class SqliteCollection : ICollection
{
    private readonly SqliteConnection _connection;
    private readonly string _tableName;

    public string Name { get; }
    public int Dimensions { get; }
    public string EmbedderIdentity { get; }

    public SqliteCollection(SqliteConnection connection, string name, int dimensions, string embedderIdentity)
    {
        _connection = connection;
        Name = name;
        Dimensions = dimensions;
        EmbedderIdentity = embedderIdentity;
        _tableName = $"collection_{name}";
    }

    public async ValueTask AddAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            ValidateEmbedding(record.Embedding);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO [{_tableName}] (id, document, metadata, embedding, dim)
                VALUES (@id, @doc, @meta, @emb, @dim)";
            
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.Parameters.AddWithValue("@doc", record.Document);
            cmd.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(record.Metadata));
            cmd.Parameters.AddWithValue("@emb", EmbeddingToBytes(record.Embedding));
            cmd.Parameters.AddWithValue("@dim", Dimensions);

            try
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                throw new InvalidOperationException($"Record with ID '{record.Id}' already exists", ex);
            }
        }
    }

    public async ValueTask UpsertAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            ValidateEmbedding(record.Embedding);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $@"
                INSERT OR REPLACE INTO [{_tableName}] (id, document, metadata, embedding, dim)
                VALUES (@id, @doc, @meta, @emb, @dim)";
            
            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.Parameters.AddWithValue("@doc", record.Document);
            cmd.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(record.Metadata));
            cmd.Parameters.AddWithValue("@emb", EmbeddingToBytes(record.Embedding));
            cmd.Parameters.AddWithValue("@dim", Dimensions);

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async ValueTask<GetResult> GetAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        int? limit = null,
        int offset = 0,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder($"SELECT id, document, metadata");
        
        if (include.HasFlag(IncludeFields.Embeddings))
        {
            sb.Append(", embedding");
        }

        sb.Append($" FROM [{_tableName}] WHERE 1=1");

        using var cmd = _connection.CreateCommand();

        if (ids != null && ids.Count > 0)
        {
            var idParams = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                var paramName = $"@id{i}";
                idParams.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, ids[i]);
            }
            sb.Append($" AND id IN ({string.Join(", ", idParams)})");
        }

        if (where != null)
        {
            sb.Append(" AND ");
            sb.Append(TranslateWhereClause(where, cmd));
        }

        if (limit.HasValue)
        {
            sb.Append($" LIMIT {limit.Value}");
        }

        if (offset > 0)
        {
            sb.Append($" OFFSET {offset}");
        }

        cmd.CommandText = sb.ToString();

        var resultIds = new List<string>();
        var resultDocs = new List<string>();
        var resultMetas = new List<IReadOnlyDictionary<string, object?>>();
        var resultEmbs = include.HasFlag(IncludeFields.Embeddings) ? new List<ReadOnlyMemory<float>>() : null;

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            resultIds.Add(reader.GetString(0));
            resultDocs.Add(reader.GetString(1));
            
            var metaJson = reader.GetString(2);
            var meta = JsonSerializer.Deserialize<Dictionary<string, object?>>(metaJson) ?? new Dictionary<string, object?>();
            resultMetas.Add(meta);

            if (include.HasFlag(IncludeFields.Embeddings))
            {
                var embBytes = (byte[])reader.GetValue(3);
                resultEmbs!.Add(BytesToEmbedding(embBytes));
            }
        }

        return new GetResult(resultIds, resultDocs, resultMetas, resultEmbs);
    }

    public async ValueTask<QueryResult> QueryAsync(
        IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings,
        int nResults = 10,
        WhereClause? where = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
        CancellationToken ct = default)
    {
        var resultIds = new List<IReadOnlyList<string>>();
        var resultDocs = new List<IReadOnlyList<string>>();
        var resultMetas = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        var resultDists = new List<IReadOnlyList<float>>();
        var resultEmbs = include.HasFlag(IncludeFields.Embeddings) ? new List<IReadOnlyList<ReadOnlyMemory<float>>>() : null;

        foreach (var queryEmb in queryEmbeddings)
        {
            ValidateEmbedding(queryEmb);

            var sb = new StringBuilder($"SELECT id, document, metadata, embedding FROM [{_tableName}]");

            using var cmd = _connection.CreateCommand();

            if (where != null)
            {
                sb.Append(" WHERE ");
                sb.Append(TranslateWhereClause(where, cmd));
            }

            cmd.CommandText = sb.ToString();

            var candidates = new List<(string id, string doc, Dictionary<string, object?> meta, ReadOnlyMemory<float> emb, float distance)>();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var id = reader.GetString(0);
                var doc = reader.GetString(1);
                var metaJson = reader.GetString(2);
                var meta = JsonSerializer.Deserialize<Dictionary<string, object?>>(metaJson) ?? new Dictionary<string, object?>();
                var embBytes = (byte[])reader.GetValue(3);
                var emb = BytesToEmbedding(embBytes);

                var distance = ComputeCosineDistance(queryEmb, emb);
                candidates.Add((id, doc, meta, emb, distance));
            }

            var topK = candidates
                .OrderBy(c => c.distance)
                .Take(nResults)
                .ToList();

            resultIds.Add(topK.Select(c => c.id).ToList());
            resultDocs.Add(topK.Select(c => c.doc).ToList());
            resultMetas.Add(topK.Select(c => (IReadOnlyDictionary<string, object?>)c.meta).ToList());
            resultDists.Add(topK.Select(c => c.distance).ToList());
            
            if (include.HasFlag(IncludeFields.Embeddings))
            {
                resultEmbs!.Add(topK.Select(c => c.emb).ToList());
            }
        }

        return new QueryResult(resultIds, resultDocs, resultMetas, resultDists, resultEmbs);
    }

    public async ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM [{_tableName}]";
        
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async ValueTask DeleteAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder($"DELETE FROM [{_tableName}] WHERE 1=1");

        using var cmd = _connection.CreateCommand();

        if (ids != null && ids.Count > 0)
        {
            var idParams = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                var paramName = $"@id{i}";
                idParams.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, ids[i]);
            }
            sb.Append($" AND id IN ({string.Join(", ", idParams)})");
        }

        if (where != null)
        {
            sb.Append(" AND ");
            sb.Append(TranslateWhereClause(where, cmd));
        }

        cmd.CommandText = sb.ToString();
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask<IReadOnlyList<EmbeddedRecord>> WakeUpAsync(
        int limit = 20,
        WhereClause? where = null,
        DateTime? sinceDate = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder($"SELECT id, document, metadata");
        
        if (include.HasFlag(IncludeFields.Embeddings))
        {
            sb.Append(", embedding");
        }

        sb.Append($" FROM [{_tableName}] WHERE 1=1");

        using var cmd = _connection.CreateCommand();

        // Add date filter if specified
        if (sinceDate.HasValue)
        {
            sb.Append(" AND json_extract(metadata, '$.timestamp') >= @sinceDate");
            cmd.Parameters.AddWithValue("@sinceDate", sinceDate.Value.Ticks);
        }

        // Add user-specified where clause
        if (where != null)
        {
            sb.Append(" AND ");
            sb.Append(TranslateWhereClause(where, cmd));
        }

        // Order by timestamp descending (most recent first)
        sb.Append(" ORDER BY json_extract(metadata, '$.timestamp') DESC");
        sb.Append($" LIMIT {limit}");

        cmd.CommandText = sb.ToString();

        var results = new List<EmbeddedRecord>();

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetString(0);
            var document = reader.GetString(1);
            var metaJson = reader.GetString(2);
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metaJson) ?? new Dictionary<string, object?>();
            
            ReadOnlyMemory<float> embedding = ReadOnlyMemory<float>.Empty;
            if (include.HasFlag(IncludeFields.Embeddings))
            {
                var embBytes = (byte[])reader.GetValue(3);
                embedding = BytesToEmbedding(embBytes);
            }

            results.Add(new EmbeddedRecord(id, document, metadata, embedding));
        }

        return results;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private string TranslateWhereClause(WhereClause clause, SqliteCommand cmd)
    {
        return clause switch
        {
            Eq eq => TranslateComparison(eq.Field, "=", eq.Value, cmd),
            NotEq ne => TranslateComparison(ne.Field, "!=", ne.Value, cmd),
            Gt gt => TranslateComparison(gt.Field, ">", gt.Value, cmd),
            Gte gte => TranslateComparison(gte.Field, ">=", gte.Value, cmd),
            Lt lt => TranslateComparison(lt.Field, "<", lt.Value, cmd),
            Lte lte => TranslateComparison(lte.Field, "<=", lte.Value, cmd),
            In inClause => TranslateIn(inClause.Field, inClause.Values, cmd, negate: false),
            NotIn notInClause => TranslateIn(notInClause.Field, notInClause.Values, cmd, negate: true),
            And and => $"({string.Join(" AND ", and.Clauses.Select(c => TranslateWhereClause(c, cmd)))})",
            Or or => $"({string.Join(" OR ", or.Clauses.Select(c => TranslateWhereClause(c, cmd)))})",
            _ => throw new UnsupportedFilterException($"Unsupported filter type: {clause.GetType().Name}")
        };
    }

    private string TranslateComparison(string field, string op, object? value, SqliteCommand cmd)
    {
        var paramName = $"@p{cmd.Parameters.Count}";
        cmd.Parameters.AddWithValue(paramName, ConvertValue(value));
        return $"json_extract(metadata, '$.{field}') {op} {paramName}";
    }

    private string TranslateIn(string field, IReadOnlyList<object?> values, SqliteCommand cmd, bool negate)
    {
        var paramNames = new List<string>();
        foreach (var value in values)
        {
            var paramName = $"@p{cmd.Parameters.Count}";
            cmd.Parameters.AddWithValue(paramName, ConvertValue(value));
            paramNames.Add(paramName);
        }

        var inList = string.Join(", ", paramNames);
        var notKeyword = negate ? "NOT " : "";
        return $"json_extract(metadata, '$.{field}') {notKeyword}IN ({inList})";
    }

    private object? ConvertValue(object? value)
    {
        if (value == null) return DBNull.Value;
        
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => DBNull.Value,
                _ => value.ToString()
            };
        }

        return value;
    }

    private void ValidateEmbedding(ReadOnlyMemory<float> embedding)
    {
        if (embedding.Length != Dimensions)
        {
            throw new DimensionMismatchException(
                $"Embedding dimension {embedding.Length} does not match collection dimension {Dimensions}");
        }
    }

    private static byte[] EmbeddingToBytes(ReadOnlyMemory<float> embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private ReadOnlyMemory<float> BytesToEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    private static float ComputeCosineDistance(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var spanA = a.Span;
        var spanB = b.Span;

        float dot = 0f;
        float magA = 0f;
        float magB = 0f;

        for (int i = 0; i < spanA.Length; i++)
        {
            dot += spanA[i] * spanB[i];
            magA += spanA[i] * spanA[i];
            magB += spanB[i] * spanB[i];
        }

        var magnitude = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        if (magnitude == 0f) return 1f;

        var cosineSimilarity = dot / magnitude;
        return 1f - cosineSimilarity;
    }
}
