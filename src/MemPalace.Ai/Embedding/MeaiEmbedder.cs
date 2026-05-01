using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// Adapter wrapping Microsoft.Extensions.AI IEmbeddingGenerator to implement MemPalace's ICustomEmbedder.
/// </summary>
public sealed class MeaiEmbedder : ICustomEmbedder
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
    /// Provider name for factory resolution (e.g., "local", "openai", "azureopenai").
    /// </summary>
    public string ProviderName => _providerName.ToLowerInvariant();

    /// <summary>
    /// Model identity combining provider and model (e.g., "ollama:nomic-embed-text" or "local:sentence-transformers/all-MiniLM-L6-v2").
    /// For local provider, the full HuggingFace model ID is used.
    /// </summary>
    public string ModelIdentity => $"{ProviderName}:{_modelName}";
    
    /// <summary>
    /// Embedder metadata for runtime introspection.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        { "provider", ProviderName },
        { "model", _modelName },
        { "source", "Microsoft.Extensions.AI" }
    };

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
