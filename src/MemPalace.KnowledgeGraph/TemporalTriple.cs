namespace MemPalace.KnowledgeGraph;

/// <summary>
/// A triple with temporal validity intervals.
/// </summary>
/// <param name="Triple">The underlying triple</param>
/// <param name="ValidFrom">Start of validity period (inclusive)</param>
/// <param name="ValidTo">End of validity period (exclusive), null if still valid</param>
/// <param name="RecordedAt">When this triple was recorded</param>
public sealed record TemporalTriple(
    Triple Triple,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset RecordedAt)
{
    /// <summary>
    /// Check if this triple is valid at a specific point in time.
    /// </summary>
    public bool IsCurrentAt(DateTimeOffset t) =>
        ValidFrom <= t && (ValidTo is null || t < ValidTo);
}
