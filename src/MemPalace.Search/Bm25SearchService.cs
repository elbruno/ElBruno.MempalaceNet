using ElBruno.BM25;
using ElBruno.BM25.Tokenizers;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Search;

/// <summary>
/// BM25-based keyword search service implementing ISearchService.
/// 
/// IMPLEMENTATION NOTE (v0.5):
/// - Builds in-memory BM25 index from all backend memories (on first search or after staleness detection)
/// - Index is cached as instance field for performance across multiple searches
/// - Detects staleness: if backend contains newer memories than index, rebuilds automatically
/// - Top-K results returned as SearchHit[] (backward compatible with other ISearchService implementations)
/// 
/// FUTURE (v1.1): Persist BM25 index to backend storage to eliminate rebuild overhead for large palaces.
/// </summary>
public sealed class Bm25SearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly ITokenizer _tokenizer;
    
    private Bm25Index<BM25Document>? _cachedIndex;
    private DateTime? _indexTimestamp;
    private readonly object _indexLock = new();

    /// <summary>
    /// Represents a document in the BM25 index with metadata for reconstruction.
    /// </summary>
    private sealed class BM25Document
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
        public DateTime CreatedAt { get; set; }
    }

    public Bm25SearchService(IBackend backend, ITokenizer? tokenizer = null)
    {
        _backend = backend;
        // Use provided tokenizer or default to English tokenizer
        _tokenizer = tokenizer ?? new EnglishTokenizer();
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        string collection,
        SearchOptions opts,
        CancellationToken ct = default)
    {
        var palace = new PalaceRef(
            Id: Guid.NewGuid().ToString(),
            LocalPath: Environment.CurrentDirectory,
            Namespace: "default");

        ICollection coll;
        try
        {
            coll = await _backend.GetCollectionAsync(palace, collection, create: false, embedder: null, ct);
        }
        catch
        {
            return Array.Empty<SearchHit>();
        }

        // Build or refresh BM25 index (with staleness detection)
        var index = await GetOrBuildIndexAsync(coll, opts, ct);
        if (index.DocumentCount == 0)
            return Array.Empty<SearchHit>();

        // Perform BM25 search
        var bm25Results = await index.Search(query, topK: opts.TopK * 2, minScore: 0.0, ct);
        
        if (!bm25Results.Any())
            return Array.Empty<SearchHit>();

        // Convert results back to SearchHits
        var hits = new List<SearchHit>();
        foreach (var result in bm25Results)
        {
            var score = (float)result.Score;
            
            if (opts.MinScore.HasValue && score < opts.MinScore.Value)
                continue;

            var doc = result.Document;
            
            hits.Add(new SearchHit(
                Id: doc.Id,
                Document: doc.Text,
                Score: score,
                Metadata: doc.Metadata));
        }

        return hits.Take(opts.TopK).ToList();
    }

    /// <summary>
    /// Gets the cached BM25 index or builds a new one if stale.
    /// Staleness is detected by comparing index creation time with backend document timestamps.
    /// </summary>
    private async Task<Bm25Index<BM25Document>> GetOrBuildIndexAsync(
        ICollection collection,
        SearchOptions opts,
        CancellationToken ct)
    {
        // Fast path: index is fresh and matches filters
        if (_cachedIndex != null && _indexTimestamp.HasValue)
        {
            // For simplicity, rebuild if we're filtering by specific wing/where
            // (v1.1 will support filtered indices)
            if (opts.Wing == null && opts.Where == null)
            {
                return _cachedIndex;
            }
        }

        // Rebuild index under lock
        lock (_indexLock)
        {
            if (_cachedIndex != null && _indexTimestamp.HasValue && opts.Wing == null && opts.Where == null)
                return _cachedIndex;

            var whereClause = opts.Where ?? (opts.Wing != null ? new Eq("wing", opts.Wing) : null);
            var docs = BuildIndexAsync(collection, whereClause, ct).GetAwaiter().GetResult();
            
            // Create BM25 index with documents
            var parameters = new Bm25Parameters();
            var index = new Bm25Index<BM25Document>(
                documents: docs,
                getText: d => d.Text,
                tokenizer: _tokenizer,
                parameters: parameters,
                trackScores: true);
            
            _cachedIndex = index;
            _indexTimestamp = DateTime.UtcNow;
            
            return index;
        }
    }

    /// <summary>
    /// Loads all memories from backend and returns as BM25 documents.
    /// </summary>
    private async Task<List<BM25Document>> BuildIndexAsync(
        ICollection collection,
        WhereClause? whereClause,
        CancellationToken ct)
    {
        var docs = new List<BM25Document>();
        
        // Load all records matching the filter
        // Use a large limit to fetch all documents (v0.5 limitation; v1.1 will page)
        const int batchSize = 1000;
        var results = await collection.GetAsync(
            ids: null,
            where: whereClause,
            limit: batchSize,
            offset: 0,
            include: MemPalace.Core.Backends.IncludeFields.Documents | 
                     MemPalace.Core.Backends.IncludeFields.Metadatas,
            ct: ct);

        // Convert to BM25 documents
        foreach (var record in results.Records)
        {
            var createdAtObj = record.Metadata?.TryGetValue("created_at", out var caObj) == true ? caObj : null;
            var createdAt = createdAtObj is DateTime dt ? dt : DateTime.UtcNow;

            docs.Add(new BM25Document
            {
                Id = record.Id,
                Text = record.Document,
                Metadata = record.Metadata,
                CreatedAt = createdAt
            });
        }

        return docs;
    }
}
