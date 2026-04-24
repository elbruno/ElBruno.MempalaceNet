namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Interface for benchmarks.
/// </summary>
public interface IBenchmark
{
    /// <summary>
    /// Gets the benchmark name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the benchmark description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Runs the benchmark.
    /// </summary>
    Task<BenchmarkResult> RunAsync(BenchmarkContext ctx, CancellationToken ct = default);
}
