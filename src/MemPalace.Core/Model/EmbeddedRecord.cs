namespace MemPalace.Core.Model;

/// <summary>
/// A record to be inserted into a collection, containing an embedding.
/// </summary>
public sealed record EmbeddedRecord(
    string Id,
    string Document,
    IReadOnlyDictionary<string, object?> Metadata,
    ReadOnlyMemory<float> Embedding);
