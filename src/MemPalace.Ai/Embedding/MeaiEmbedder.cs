using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// Adapter wrapping Microsoft.Extensions.AI IEmbeddingGenerator to implement MemPalace's IEmbedder.
/// </summary>
public sealed class MeaiEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly string _providerName;
    private readonly string _modelName;
    private int? _dimensions;

    public MeaiEmbedder(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        string providerName,
        string modelName)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    /// <summary>
    /// Model identity combining provider and model (e.g., "ollama:nomic-embed-text" or "local:sentence-transformers/all-MiniLM-L6-v2").
    /// For local provider, the full HuggingFace model ID is used.
    /// </summary>
    public string ModelIdentity => $"{_providerName.ToLowerInvariant()}:{_modelName}";

    /// <summary>
    /// Embedding dimensions (inferred from first embedding call).
    /// </summary>
    public int Dimensions
    {
        get
        {
            if (!_dimensions.HasValue)
            {
                throw new InvalidOperationException(
                    "Dimensions not yet known. Call EmbedAsync at least once.");
            }
            return _dimensions.Value;
        }
    }

    /// <summary>
    /// Embeds a batch of texts into vectors using the underlying M.E.AI generator.
    /// </summary>
    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return Array.Empty<ReadOnlyMemory<float>>();
        }

        var embeddings = await _generator.GenerateAsync(texts, cancellationToken: ct);
        
        var results = new List<ReadOnlyMemory<float>>(embeddings.Count);
        foreach (var embedding in embeddings)
        {
            var vector = embedding.Vector;
            
            // Infer dimensions from first embedding
            if (!_dimensions.HasValue)
            {
                _dimensions = vector.Length;
            }
            
            results.Add(vector);
        }

        return results;
    }
}
