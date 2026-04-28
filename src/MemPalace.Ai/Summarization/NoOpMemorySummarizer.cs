using MemPalace.Core.Backends;

namespace MemPalace.Ai.Summarization;

/// <summary>
/// No-op summarizer for deployments without LLM support.
/// Returns null to signal graceful degradation.
/// </summary>
public sealed class NoOpMemorySummarizer : IMemorySummarizer
{
    /// <summary>
    /// Returns null to indicate no summarization is available.
    /// </summary>
    public ValueTask<string?> SummarizeAsync(GetResult memories, CancellationToken ct = default)
    {
        return ValueTask.FromResult<string?>(null);
    }
}
