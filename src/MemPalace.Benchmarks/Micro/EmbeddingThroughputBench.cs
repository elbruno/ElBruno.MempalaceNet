using BenchmarkDotNet.Attributes;
using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;

namespace MemPalace.Benchmarks.Micro;

/// <summary>
/// Micro-benchmarks for embedding throughput.
/// </summary>
[MemoryDiagnoser]
public class EmbeddingThroughputBench
{
    private IEmbedder? _embedder;
    private List<string> _texts = new();

    [Params(10, 100)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _embedder = new DeterministicEmbedder(384);
        
        // Generate sample texts
        _texts = Enumerable.Range(0, BatchSize)
            .Select(i => $"This is sample text number {i} for benchmarking embedding throughput.")
            .ToList();
    }

    [Benchmark]
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatch()
    {
        if (_embedder == null)
            throw new InvalidOperationException("Embedder not initialized");
        
        return await _embedder.EmbedAsync(_texts);
    }
}
