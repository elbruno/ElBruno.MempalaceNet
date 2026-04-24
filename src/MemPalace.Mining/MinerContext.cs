namespace MemPalace.Mining;

/// <summary>
/// Context passed to miners containing source path and options.
/// </summary>
public sealed record MinerContext(
    string SourcePath,
    string? Wing,
    IReadOnlyDictionary<string, string?> Options);
