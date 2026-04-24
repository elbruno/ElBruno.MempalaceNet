using Microsoft.Extensions.AI;

namespace MemPalace.Ai.Rerank;

/// <summary>
/// LLM-based reranker using Microsoft.Extensions.AI IChatClient.
/// </summary>
public sealed class LlmReranker : IReranker
{
    private readonly IChatClient _chatClient;

    public LlmReranker(IChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <summary>
    /// Reranks candidates using LLM-based scoring.
    /// </summary>
    public async ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return Array.Empty<RankedHit>();
        }

        // PHASE 9: full prompt and LLM-based reranking logic
        // TODO: Implement proper reranking prompt and score parsing
        // For now, return candidates as-is (pass-through)
        return candidates;
    }
}
