namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Represents one retrievable document in a benchmark corpus.
/// </summary>
/// <param name="Id">Stable corpus identifier used for scoring.</param>
/// <param name="Document">Text stored in the benchmark collection.</param>
/// <param name="Metadata">Additional metadata carried with the document.</param>
public sealed record CorpusDocument(
    string Id,
    string Document,
    IReadOnlyDictionary<string, object?> Metadata);
