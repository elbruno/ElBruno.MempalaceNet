using MemPalace.Core.Model;

namespace MemPalace.Core.Backends;

/// <summary>
/// Backend interface for storage systems. Factory pattern: addressed by PalaceRef.
/// </summary>
public interface IBackend : IAsyncDisposable
{
    /// <summary>
    /// Gets or creates a collection in the specified palace.
    /// Throws PalaceNotFoundException if create=false and palace doesn't exist.
    /// Throws EmbedderIdentityMismatchException if embedder doesn't match existing collection.
    /// </summary>
    ValueTask<ICollection> GetCollectionAsync(
        PalaceRef palace,
        string collectionName,
        bool create = false,
        IEmbedder? embedder = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all collection names in a palace.
    /// </summary>
    ValueTask<IReadOnlyList<string>> ListCollectionsAsync(
        PalaceRef palace,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a collection from a palace.
    /// </summary>
    ValueTask DeleteCollectionAsync(
        PalaceRef palace,
        string name,
        CancellationToken ct = default);

    /// <summary>
    /// Checks backend health.
    /// </summary>
    ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default);
}
