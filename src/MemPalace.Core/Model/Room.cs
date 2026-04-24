namespace MemPalace.Core.Model;

/// <summary>
/// Represents a room within a wing - a mid-level organizational unit grouped by topic.
/// </summary>
public sealed record Room(string Name, string Wing, string? Topic = null);
