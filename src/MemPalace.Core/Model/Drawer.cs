namespace MemPalace.Core.Model;

/// <summary>
/// Represents a drawer - the atomic storage unit containing content and metadata.
/// </summary>
public sealed record Drawer(
    string Id,
    string Room,
    string Wing,
    string Content,
    IReadOnlyDictionary<string, object?> Metadata,
    DateTimeOffset CreatedAt);
