using MemPalace.Ai.Rerank;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Search;

/// <summary>
/// Hybrid search combining vector similarity and BM25 keyword matching using Reciprocal Rank Fusion.
/// Optionally applies LLM-based reranking for improved relevance.
/// </summary>
public sealed class HybridSearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly Bm25SearchService _bm25Service;
    private readonly IReranker? _reranker;

    public HybridSearchService(
        IBackend backend,
        IEmbedder embedder,
        Bm25SearchService? bm25Service = null,
        IReranker? reranker = null)
    {
        _backend = backend;
        _embedder = embedder;
        _bm25Service = bm25Service ?? new Bm25SearchService(backend);
        _reranker = reranker;
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
            coll = await _backend.GetCollectionAsync(palace, collection, create: false, _embedder, ct);
        }
        catch
        {
            return Array.Empty<SearchHit>();
        }

        // Perform vector search
        var queryEmbedding = await _embedder.EmbedAsync(new[] { query }, ct);
        var vectorResults = await coll.QueryAsync(
            queryEmbeddings: queryEmbedding,
            nResults: opts.TopK * 2, // Get more candidates for fusion
            where: opts.Where ?? (opts.Wing != null ? new Eq("wing", opts.Wing) : null),
            include: IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
            ct: ct);

        if (vectorResults.Ids.Count == 0 || vectorResults.Ids[0].Count == 0)
            return Array.Empty<SearchHit>();

        // Perform BM25 search
        var bm25Results = await _bm25Service.SearchAsync(query, collection, opts with { TopK = opts.TopK * 2 }, ct);

        // Reciprocal Rank Fusion (RRF)
        const int k = 60; // RRF constant
        var rrfScores = new Dictionary<string, float>();

        // Add vector scores
        for (var i = 0; i < vectorResults.Ids[0].Count; i++)
        {
            var id = vectorResults.Ids[0][i];
            var distance = vectorResults.Distances[0][i];
            var score = 1.0f - distance; // Convert distance to similarity score
            var rank = i + 1;
            rrfScores[id] = 1.0f / (k + rank);
        }

        // Add BM25 scores
        var bm25Ranked = bm25Results.OrderByDescending(h => h.Score).ToList();
        for (var i = 0; i < bm25Ranked.Count; i++)
        {
            var id = bm25Ranked[i].Id;
            var rank = i + 1;
            rrfScores[id] = rrfScores.GetValueOrDefault(id, 0) + 1.0f / (k + rank);
        }

        // Sort by fused score and take top-K
        var fusedResults = rrfScores
            .OrderByDescending(kv => kv.Value)
            .Take(opts.TopK)
            .ToList();

        var hits = new List<SearchHit>();
        foreach (var (id, score) in fusedResults)
        {
            if (opts.MinScore.HasValue && score < opts.MinScore.Value)
                continue;

            var index = Array.IndexOf(vectorResults.Ids[0].ToArray(), id);
            if (index < 0)
                continue;

            var metadata = new Dictionary<string, object?>(vectorResults.Metadatas[0][index])
            {
                ["sources"] = new[] { "vector", "bm25" }
            };

            hits.Add(new SearchHit(
                Id: id,
                Document: vectorResults.Documents[0][index],
                Score: score,
                Metadata: metadata));
        }

        // Apply reranking if requested
        if (opts.Rerank && _reranker != null && hits.Count > 0)
        {
            var rankedHits = hits.Select(h => new RankedHit(h.Id, h.Document, h.Score)).ToList();
            var reranked = await _reranker.RerankAsync(query, rankedHits, ct);

            hits = reranked.Select(rh =>
            {
                var original = hits.First(h => h.Id == rh.Id);
                return new SearchHit(rh.Id, rh.Document, rh.Score, original.Metadata);
            }).ToList();
        }

        return hits;
    }
}
