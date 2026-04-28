using MemPalace.Core.Backends;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;

namespace CustomBackendTemplate;

/// <summary>
/// Example custom backend implementation.
/// 
/// This demonstrates the minimal contract for IBackend and ICollection.
/// It uses a simple in-memory dictionary as storage (not production-ready).
/// 
/// To adapt this for your storage system (Postgres, Qdrant, etc.):
/// 1. Replace Dictionary with your client/API calls
/// 2. Translate ICollection methods to your storage operations
/// 3. Implement proper error handling and validation
/// 4. Pass BackendConformanceTests from MemPalace.Tests
/// </summary>
public sealed class CustomBackend : IBackend
{
    // Simple in-memory storage: palace.Id -> collection name -> ICollection
    private readonly Dictionary<string, Dictionary<string, CustomCollection>> _palaces = new();
    private bool _disposed;

    public async ValueTask<ICollection> GetCollectionAsync(
        PalaceRef palace,
        string collectionName,
        bool create = false,
        IEmbedder? embedder = null,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        // Ensure palace exists in storage
        if (!_palaces.ContainsKey(palace.Id))
        {
            if (!create)
                throw new PalaceNotFoundException($"Palace '{palace.Id}' not found");
            
            if (embedder == null)
                throw new ArgumentException("Embedder required when creating palace");
            
            _palaces[palace.Id] = new();
        }

        var palaceCollections = _palaces[palace.Id];

        // Ensure collection exists
        if (!palaceCollections.ContainsKey(collectionName))
        {
            if (!create)
                throw new PalaceNotFoundException($"Collection '{collectionName}' not found in palace '{palace.Id}'");
            
            if (embedder == null)
                throw new ArgumentException("Embedder required when creating collection");
            
            // Create new collection
            var collection = new CustomCollection(
                collectionName,
                embedder.Dimensions,
                embedder.ModelIdentity);
            
            palaceCollections[collectionName] = collection;
        }

        var existingCollection = palaceCollections[collectionName];

        // Validate embedder matches (guard against mismatch)
        if (embedder != null && embedder.ModelIdentity != existingCollection.EmbedderIdentity)
            throw new EmbedderIdentityMismatchException(
                $"Embedder mismatch: expected '{existingCollection.EmbedderIdentity}', got '{embedder.ModelIdentity}'");

        return await ValueTask.FromResult<ICollection>(existingCollection);
    }

    public async ValueTask<IReadOnlyList<string>> ListCollectionsAsync(
        PalaceRef palace,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        if (!_palaces.TryGetValue(palace.Id, out var palaceCollections))
            return new List<string>();

        return await ValueTask.FromResult<IReadOnlyList<string>>(
            palaceCollections.Keys.ToList());
    }

    public async ValueTask DeleteCollectionAsync(
        PalaceRef palace,
        string name,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        if (_palaces.TryGetValue(palace.Id, out var palaceCollections))
        {
            palaceCollections.Remove(name);
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        
        // Simple health check: just verify backend is responsive
        return await ValueTask.FromResult(HealthStatus.Healthy("CustomBackend is operational"));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Clean up all collections
        foreach (var palaceCollections in _palaces.Values)
        {
            foreach (var collection in palaceCollections.Values)
            {
                await collection.DisposeAsync();
            }
        }

        _palaces.Clear();
        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new BackendClosedException("CustomBackend has been disposed");
    }
}

/// <summary>
/// Custom collection implementation.
/// Stores records in memory using a simple dictionary.
/// </summary>
public sealed class CustomCollection : ICollection
{
    private readonly Dictionary<string, EmbeddedRecord> _records = new();
    private bool _disposed;

    public string Name { get; }
    public int Dimensions { get; }
    public string EmbedderIdentity { get; }

    public CustomCollection(string name, int dimensions, string embedderIdentity)
    {
        Name = name;
        Dimensions = dimensions;
        EmbedderIdentity = embedderIdentity;
    }

    public async ValueTask AddAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        // Validate dimensions
        foreach (var record in records)
        {
            if (record.Embedding.Length != Dimensions)
                throw new DimensionMismatchException(
                    $"Embedding dimension mismatch: expected {Dimensions}, got {record.Embedding.Length}");

            // Add will throw if ID exists
            if (_records.ContainsKey(record.Id))
                throw new InvalidOperationException($"Record with ID '{record.Id}' already exists");

            _records[record.Id] = record;
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask UpsertAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        foreach (var record in records)
        {
            if (record.Embedding.Length != Dimensions)
                throw new DimensionMismatchException(
                    $"Embedding dimension mismatch: expected {Dimensions}, got {record.Embedding.Length}");

            _records[record.Id] = record;  // Upsert: insert or update
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask<GetResult> GetAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        int? limit = null,
        int offset = 0,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var results = new List<EmbeddedRecord>();

        // Filter by IDs
        if (ids != null && ids.Count > 0)
        {
            foreach (var id in ids)
            {
                if (_records.TryGetValue(id, out var record))
                    results.Add(record);
            }
        }
        else
        {
            // Get all records if no ID filter
            results.AddRange(_records.Values);
        }

        // Apply metadata where clause (simplified: just return all)
        // In a real backend, implement proper filter evaluation here
        if (where != null)
        {
            // For this template, we don't implement where clause filtering
            // See MemPalace.Backends.Sqlite for a real implementation
            throw new UnsupportedFilterException("This template backend does not support where clauses");
        }

        // Apply pagination
        results = results
            .Skip(offset)
            .Take(limit ?? results.Count)
            .ToList();

        // Build response based on include flags
        var ids_list = results.Select(r => r.Id).ToList();
        var docs = (include & IncludeFields.Documents) != 0
            ? results.Select(r => r.Document).ToList()
            : new List<string>();
        var metadata = (include & IncludeFields.Metadatas) != 0
            ? results.Select(r => r.Metadata).ToList()
            : new List<IReadOnlyDictionary<string, object?>>();
        var embeddings = (include & IncludeFields.Embeddings) != 0
            ? results.Select(r => r.Embedding).ToList()
            : null;

        return await ValueTask.FromResult(
            new GetResult(ids_list, docs, metadata, embeddings));
    }

    public async ValueTask<QueryResult> QueryAsync(
        IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings,
        int nResults = 10,
        WhereClause? where = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var queryResults = new List<IReadOnlyList<string>>();
        var queryDocs = new List<IReadOnlyList<string>>();
        var queryMetas = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        var queryDists = new List<IReadOnlyList<float>>();
        var queryEmbeds = (include & IncludeFields.Embeddings) != 0
            ? new List<IReadOnlyList<ReadOnlyMemory<float>>>()
            : null;

        // Process each query
        foreach (var queryEmbedding in queryEmbeddings)
        {
            var candidates = new List<(string Id, float Distance, EmbeddedRecord Record)>();

            // Compute distance for each stored record
            foreach (var record in _records.Values)
            {
                var distance = CosineSimilarityDistance(queryEmbedding, record.Embedding);
                candidates.Add((record.Id, distance, record));
            }

            // Sort by distance (ascending: lowest distance = highest similarity)
            candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            // Take top-K results
            var topK = candidates.Take(nResults).ToList();

            // Build result lists
            var ids = topK.Select(c => c.Id).ToList();
            var docs = (include & IncludeFields.Documents) != 0
                ? topK.Select(c => c.Record.Document).ToList()
                : new List<string>();
            var metas = (include & IncludeFields.Metadatas) != 0
                ? topK.Select(c => c.Record.Metadata).ToList()
                : new List<IReadOnlyDictionary<string, object?>>();
            var dists = (include & IncludeFields.Distances) != 0
                ? topK.Select(c => c.Distance).ToList()
                : new List<float>();
            var embeds = queryEmbeds != null
                ? topK.Select(c => c.Record.Embedding).ToList()
                : null;

            queryResults.Add(ids);
            queryDocs.Add(docs);
            queryMetas.Add(metas);
            queryDists.Add(dists);
            queryEmbeds?.Add(embeds!);
        }

        return await ValueTask.FromResult(
            new QueryResult(queryResults, queryDocs, queryMetas, queryDists, queryEmbeds));
    }

    public async ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        return await ValueTask.FromResult((long)_records.Count);
    }

    public async ValueTask DeleteAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        if (ids != null && ids.Count > 0)
        {
            foreach (var id in ids)
            {
                _records.Remove(id);
            }
        }

        if (where != null)
        {
            throw new UnsupportedFilterException("This template backend does not support where clauses");
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _records.Clear();
        _disposed = true;
        await ValueTask.CompletedTask;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new BackendClosedException("CustomCollection has been disposed");
    }

    /// <summary>
    /// Computes cosine similarity distance: 1 - (dot_product / (magnitude_a * magnitude_b))
    /// Lower values indicate higher similarity.
    /// </summary>
    private static float CosineSimilarityDistance(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (int i = 0; i < aSpan.Length; i++)
        {
            dotProduct += aSpan[i] * bSpan[i];
            magnitudeA += aSpan[i] * aSpan[i];
            magnitudeB += bSpan[i] * bSpan[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 1f;  // Max distance if any vector is zero

        return 1f - (dotProduct / (magnitudeA * magnitudeB));
    }
}
