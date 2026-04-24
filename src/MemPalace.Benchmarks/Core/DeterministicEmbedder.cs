using MemPalace.Core.Backends;

namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Simple deterministic embedder for benchmarking (doesn't require actual model).
/// Generates embeddings based on text hash for reproducibility.
/// </summary>
public sealed class DeterministicEmbedder : IEmbedder
{
    public string ModelIdentity => $"deterministic-{Dimensions}";
    
    public int Dimensions { get; }

    public DeterministicEmbedder(int dimensions)
    {
        Dimensions = dimensions;
    }

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            var hash = text.GetHashCode();
            var vector = new float[Dimensions];
            var rng = new Random(hash);
            for (var i = 0; i < Dimensions; i++)
            {
                vector[i] = (float)rng.NextDouble();
            }
            
            // Normalize to unit length
            var norm = Math.Sqrt(vector.Sum(x => x * x));
            for (var i = 0; i < Dimensions; i++)
            {
                vector[i] /= (float)norm;
            }
            
            results.Add(vector);
        }
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }
}
