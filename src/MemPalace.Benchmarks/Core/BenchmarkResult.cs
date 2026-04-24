namespace MemPalace.Benchmarks.Core;

/// <summary>
/// Result of a benchmark run.
/// </summary>
/// <param name="BenchmarkName">Name of the benchmark.</param>
/// <param name="TotalQueries">Total number of queries executed.</param>
/// <param name="Correct">Number of correct retrievals (at least one relevant in top-k).</param>
/// <param name="Recall">Recall at k (0.0 to 1.0).</param>
/// <param name="Precision">Precision at k (0.0 to 1.0).</param>
/// <param name="F1">F1 score.</param>
/// <param name="NdcgAt10">Normalized Discounted Cumulative Gain at k=10.</param>
/// <param name="TotalDuration">Total time taken for the benchmark.</param>
/// <param name="ExtraMetrics">Additional metrics specific to the benchmark.</param>
public record BenchmarkResult(
    string BenchmarkName,
    int TotalQueries,
    int Correct,
    double Recall,
    double Precision,
    double F1,
    double NdcgAt10,
    TimeSpan TotalDuration,
    IReadOnlyDictionary<string, double> ExtraMetrics);
