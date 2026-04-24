using System.Collections.Concurrent;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;

namespace MemPalace.Core.Backends.InMemory;

/// <summary>
/// In-memory backend for testing. Implements IBackend + ICollection.
/// </summary>
public sealed class InMemoryBackend : IBackend
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, InMemoryCollection>> _palaces = new();
    private bool _disposed;

    public ValueTask<ICollection> GetCollectionAsync(
        PalaceRef palace,
        string collectionName,
        bool create = false,
        IEmbedder? embedder = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var palaceCollections = _palaces.GetOrAdd(palace.Id, _ => new ConcurrentDictionary<string, InMemoryCollection>());

        if (!palaceCollections.TryGetValue(collectionName, out var collection))
        {
            if (!create)
                throw new PalaceNotFoundException($"Palace '{palace.Id}' or collection '{collectionName}' not found.");

            if (embedder == null)
                throw new ArgumentNullException(nameof(embedder), "Embedder required when creating a collection.");

            collection = new InMemoryCollection(collectionName, embedder.Dimensions, embedder.ModelIdentity);
            palaceCollections[collectionName] = collection;
        }
        else if (embedder != null && embedder.ModelIdentity != collection.EmbedderIdentity)
        {
            throw new EmbedderIdentityMismatchException(
                $"Collection '{collectionName}' was created with embedder '{collection.EmbedderIdentity}', but embedder '{embedder.ModelIdentity}' was provided.");
        }

        return ValueTask.FromResult<ICollection>(collection);
    }

    public ValueTask<IReadOnlyList<string>> ListCollectionsAsync(PalaceRef palace, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (!_palaces.TryGetValue(palace.Id, out var palaceCollections))
            return ValueTask.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        return ValueTask.FromResult<IReadOnlyList<string>>(palaceCollections.Keys.ToArray());
    }

    public ValueTask DeleteCollectionAsync(PalaceRef palace, string name, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_palaces.TryGetValue(palace.Id, out var palaceCollections))
            palaceCollections.TryRemove(name, out _);

        return ValueTask.CompletedTask;
    }

    public ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return ValueTask.FromResult(HealthStatus.Healthy("In-memory backend is operational."));
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _palaces.Clear();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new BackendClosedException();
    }
}
