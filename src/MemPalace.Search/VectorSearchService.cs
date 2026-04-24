using MemPalace.Ai.Rerank;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Search;

/// <summary>
/// Vector-based search service using embeddings.
/// </summary>
public sealed class VectorSearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly IReranker? _reranker;

    public VectorSearchService(IBackend backend, IEmbedder embedder, IReranker? reranker = null)
    {
        _backend = backend;
        _embedder = embedder;
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

        // Embed query
        var queryEmbedding = await _embedder.EmbedAsync(new[] { query }, ct);
        
        // Query backend
        var result = await coll.QueryAsync(
            queryEmbeddings: queryEmbedding,
            nResults: opts.TopK,
            where: opts.Where ?? (opts.Wing != null ? new Eq("wing", opts.Wing) : null),
            include: IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
            ct: ct);

        if (result.Ids.Count == 0 || result.Ids[0].Count == 0)
            return Array.Empty<SearchHit>();

        // Convert to SearchHits
        var hits = new List<SearchHit>();
        for (var i = 0; i < result.Ids[0].Count; i++)
        {
            var distance = result.Distances[0][i];
            var score = 1.0f - distance; // Convert distance to similarity score
            
            if (opts.MinScore.HasValue && score < opts.MinScore.Value)
                continue;

            hits.Add(new SearchHit(
                Id: result.Ids[0][i],
                Document: result.Documents[0][i],
                Score: score,
                Metadata: result.Metadatas[0][i]));
        }

        // Rerank if requested
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
