using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Search;

/// <summary>
/// Hybrid search combining vector similarity and keyword matching using Reciprocal Rank Fusion.
/// Note: v0.1 uses simple token-overlap scoring for keyword component (not full BM25).
/// </summary>
public sealed class HybridSearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;

    public HybridSearchService(IBackend backend, IEmbedder embedder)
    {
        _backend = backend;
        _embedder = embedder;
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

        // Vector search
        var queryEmbedding = await _embedder.EmbedAsync(new[] { query }, ct);
        var vectorResults = await coll.QueryAsync(
            queryEmbeddings: queryEmbedding,
            nResults: opts.TopK * 2, // Get more candidates for fusion
            where: opts.Where ?? (opts.Wing != null ? new Eq("wing", opts.Wing) : null),
            include: IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
            ct: ct);

        if (vectorResults.Ids.Count == 0 || vectorResults.Ids[0].Count == 0)
            return Array.Empty<SearchHit>();

        // Keyword search (simple token overlap)
        var queryTokens = Tokenize(query);
        var keywordScores = new Dictionary<string, float>();
        
        for (var i = 0; i < vectorResults.Ids[0].Count; i++)
        {
            var doc = vectorResults.Documents[0][i];
            var docTokens = Tokenize(doc);
            var overlap = queryTokens.Intersect(docTokens, StringComparer.OrdinalIgnoreCase).Count();
            var score = (float)overlap / Math.Max(queryTokens.Count, 1);
            keywordScores[vectorResults.Ids[0][i]] = score;
        }

        // Reciprocal Rank Fusion
        const int k = 60; // RRF constant
        var rrfScores = new Dictionary<string, float>();
        
        // Add vector scores
        for (var i = 0; i < vectorResults.Ids[0].Count; i++)
        {
            var id = vectorResults.Ids[0][i];
            var rank = i + 1;
            rrfScores[id] = 1.0f / (k + rank);
        }

        // Add keyword scores
        var keywordRanked = keywordScores.OrderByDescending(kv => kv.Value).ToList();
        for (var i = 0; i < keywordRanked.Count; i++)
        {
            var id = keywordRanked[i].Key;
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
                ["sources"] = new[] { "vector", "keyword" }
            };

            hits.Add(new SearchHit(
                Id: id,
                Document: vectorResults.Documents[0][index],
                Score: score,
                Metadata: metadata));
        }

        return hits;
    }

    private static List<string> Tokenize(string text)
    {
        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '_' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 2) // Skip very short tokens
            .ToList();
    }
}
