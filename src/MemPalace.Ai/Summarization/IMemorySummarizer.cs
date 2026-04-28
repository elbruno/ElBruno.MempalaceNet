using MemPalace.Core.Backends;

namespace MemPalace.Ai.Summarization;

/// <summary>
/// Abstraction for generating natural language summaries from memory lists.
/// </summary>
public interface IMemorySummarizer
{
    /// <summary>
    /// Generates a conversational summary from recent memories.
    /// </summary>
    /// <param name="memories">The memories to summarize (chronologically ordered).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A natural language summary, or null if summarization is unavailable.</returns>
    ValueTask<string?> SummarizeAsync(
        GetResult memories,
        CancellationToken ct = default);
}
