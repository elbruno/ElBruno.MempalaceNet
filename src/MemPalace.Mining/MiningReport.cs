namespace MemPalace.Mining;

/// <summary>
/// Report of a mining run.
/// </summary>
public sealed record MiningReport(
    long ItemsMined,
    int Batches,
    long Embedded,
    long Upserted,
    long Skipped,
    IReadOnlyList<string> Errors,
    TimeSpan Elapsed);
