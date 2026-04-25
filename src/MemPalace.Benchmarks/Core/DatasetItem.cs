namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Represents a single item in a benchmark dataset.
/// </summary>
/// <param name="Id">Unique identifier for the item.</param>
/// <param name="Question">The query or question.</param>
/// <param name="ExpectedAnswer">Expected answer (may be empty if using relevance judgments).</param>
/// <param name="RelevantMemoryIds">IDs of memories that are relevant to this question.</param>
/// <param name="Metadata">Additional metadata (e.g., session_id, turn, difficulty).</param>
/// <param name="CorpusDocuments">Optional per-query corpus documents for fresh-haystack benchmarks.</param>
public record DatasetItem(
    string Id,
    string Question,
    string ExpectedAnswer,
    IReadOnlyList<string> RelevantMemoryIds,
    IReadOnlyDictionary<string, object?> Metadata,
    IReadOnlyList<CorpusDocument>? CorpusDocuments = null);
