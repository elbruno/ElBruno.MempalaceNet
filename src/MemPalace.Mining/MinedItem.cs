namespace MemPalace.Mining;

/// <summary>
/// Represents a single item extracted by a miner.
/// </summary>
public sealed record MinedItem(
    string Id,
    string Content,
    IReadOnlyDictionary<string, object?> Metadata);
