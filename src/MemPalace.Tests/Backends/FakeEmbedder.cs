using MemPalace.Core.Backends;

namespace MemPalace.Tests.Backends;

/// <summary>
/// Fake embedder for testing - deterministic hash-based embedding generation.
/// </summary>
public sealed class FakeEmbedder : IEmbedder
{
    public string ModelIdentity { get; }
    public int Dimensions { get; }

    public FakeEmbedder(string modelIdentity = "fake-embedder-v1", int dimensions = 128)
    {
        ModelIdentity = modelIdentity;
        Dimensions = dimensions;
    }

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();
        
        foreach (var text in texts)
        {
            var hash = text.GetHashCode();
            var embedding = new float[Dimensions];
            var rng = new Random(hash);
            
            for (int i = 0; i < Dimensions; i++)
                embedding[i] = (float)rng.NextDouble();
            
            // Normalize
            float magnitude = 0f;
            for (int i = 0; i < Dimensions; i++)
                magnitude += embedding[i] * embedding[i];
            magnitude = MathF.Sqrt(magnitude);
            
            for (int i = 0; i < Dimensions; i++)
                embedding[i] /= magnitude;
            
            embeddings.Add(embedding);
        }
        
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(embeddings);
    }
}
