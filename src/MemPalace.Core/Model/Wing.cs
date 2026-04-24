namespace MemPalace.Core.Model;

/// <summary>
/// Represents a wing within a palace - a top-level organizational unit.
/// </summary>
public sealed record Wing(string Name, string? Description = null);
