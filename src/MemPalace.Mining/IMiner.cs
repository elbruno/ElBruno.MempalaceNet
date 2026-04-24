namespace MemPalace.Mining;

/// <summary>
/// Interface for mining data sources into MinedItems.
/// </summary>
public interface IMiner
{
    /// <summary>
    /// The name of this miner.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Mines items from the source specified in the context.
    /// </summary>
    IAsyncEnumerable<MinedItem> MineAsync(MinerContext ctx, CancellationToken ct = default);
}
