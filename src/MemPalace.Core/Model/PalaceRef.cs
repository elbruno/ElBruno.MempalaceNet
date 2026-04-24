namespace MemPalace.Core.Model;

/// <summary>
/// Identifies a palace by ID, optional local path, and optional namespace.
/// </summary>
public sealed record PalaceRef(string Id, string? LocalPath = null, string? Namespace = null);
