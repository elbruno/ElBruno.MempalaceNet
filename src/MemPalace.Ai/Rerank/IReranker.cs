namespace MemPalace.Ai.Rerank;

/// <summary>
/// Interface for reranking search results using LLM or other methods.
/// </summary>
public interface IReranker
{
    /// <summary>
    /// Reranks a list of candidate hits based on relevance to the query.
    /// </summary>
    ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default);
}
