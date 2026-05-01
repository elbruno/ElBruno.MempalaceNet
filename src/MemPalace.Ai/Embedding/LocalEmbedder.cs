using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.LocalEmbeddings.Extensions;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// Local embedder wrapping ElBruno.LocalEmbeddings ONNX runtime.
/// Provides offline-first embedding generation with no API keys required.
/// </summary>
public sealed class LocalEmbedder : IEmbedder, IDisposable
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly string _modelName;
    private readonly ServiceProvider _serviceProvider;
    private int? _dimensions;

    /// <summary>
    /// Creates a new LocalEmbedder with the specified model configuration.
    /// </summary>
    /// <param name="modelName">HuggingFace model ID (default: sentence-transformers/all-MiniLM-L6-v2)</param>
    /// <param name="maxSequenceLength">Maximum token sequence length (default: 256)</param>
    public LocalEmbedder(
        string modelName = "sentence-transformers/all-MiniLM-L6-v2",
        int maxSequenceLength = 256)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));
        }

        if (maxSequenceLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSequenceLength),
                maxSequenceLength,
                "Max sequence length must be positive.");
        }

        _modelName = modelName;

        // Create ServiceProvider for ElBruno.LocalEmbeddings
        var services = new ServiceCollection();
        services.AddLocalEmbeddings(options =>
        {
            options.ModelName = modelName;
            options.MaxSequenceLength = maxSequenceLength;
        });
        _serviceProvider = services.BuildServiceProvider();

        _generator = _serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    }

    /// <summary>
    /// Model identity: "local:{model-name}"
    /// </summary>
    public string ModelIdentity => $"local:{_modelName}";

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
    /// Embeds a batch of texts into vectors using the local ONNX model.
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

    /// <summary>
    /// Disposes the local embedder and releases ONNX resources.
    /// </summary>
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
