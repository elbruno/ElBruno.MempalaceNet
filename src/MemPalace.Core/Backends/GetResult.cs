namespace MemPalace.Core.Backends;

/// <summary>
/// Result from a get operation. Single-level lists (not nested like QueryResult).
/// </summary>
public sealed record GetResult(
    IReadOnlyList<string> Ids,
    IReadOnlyList<string> Documents,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Metadatas,
    IReadOnlyList<ReadOnlyMemory<float>>? Embeddings = null);
