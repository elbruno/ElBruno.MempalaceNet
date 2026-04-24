namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Context for running a benchmark.
/// </summary>
/// <param name="DatasetPath">Path to the dataset file (JSONL).</param>
/// <param name="PalacePath">Path to the palace storage directory.</param>
/// <param name="Services">Service provider for accessing backend, embedder, etc.</param>
/// <param name="MaxItems">Optional limit on number of dataset items to process.</param>
public record BenchmarkContext(
    string DatasetPath,
    string PalacePath,
    IServiceProvider Services,
    int? MaxItems = null);
