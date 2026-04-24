namespace MemPalace.Core.Backends;

/// <summary>
/// Abstraction for embedding models. Implementations live in MemPalace.Ai.
/// </summary>
public interface IEmbedder
{
    /// <summary>
    /// Unique identifier for the embedding model (e.g., "nomic-embed-text-v1.5").
    /// </summary>
    string ModelIdentity { get; }

    /// <summary>
    /// The dimensionality of the embeddings produced by this model.
    /// </summary>
    int Dimensions { get; }

    /// <summary>
    /// Embeds a list of texts into vectors.
    /// </summary>
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
