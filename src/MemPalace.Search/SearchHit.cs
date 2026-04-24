namespace MemPalace.Search;

/// <summary>
/// Represents a search result.
/// </summary>
public sealed record SearchHit(
    string Id,
    string Document,
    float Score,
    IReadOnlyDictionary<string, object?>? Metadata);
