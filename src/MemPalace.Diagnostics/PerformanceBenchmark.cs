namespace MemPalace.Diagnostics;

/// <summary>
/// Tracks performance metrics and validates SLA compliance for operations.
/// </summary>
/// <remarks>
/// <para>
/// This class allows recording latency measurements for named operations and calculating
/// percentile statistics (P50, P95, P99, P100). It supports SLA validation by comparing
/// P95 latency against configured thresholds.
/// </para>
/// <para>
/// Example usage for OpenClawNet SLA tracking:
/// - Semantic re-rank: &lt;100ms P95
/// - Health check: &lt;50ms P95
/// - Total enrichment: &lt;200ms P95 (40% of 500ms agent spawn)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var benchmark = new PerformanceBenchmark();
/// 
/// // Record latencies
/// for (int i = 0; i &lt; 100; i++)
/// {
///     var sw = Stopwatch.StartNew();
///     await PerformOperation();
///     benchmark.RecordLatency("enrichment", sw.Elapsed);
/// }
/// 
/// // Validate SLA
/// var slaPass = benchmark.ValidateSLA("enrichment", TimeSpan.FromMilliseconds(200));
/// 
/// // Generate report
/// var report = benchmark.GenerateReport();
/// Console.WriteLine(report.ToMarkdown());
/// </code>
/// </example>
public class PerformanceBenchmark
{
    private readonly Dictionary<string, List<TimeSpan>> _latencies = new();
    private readonly Dictionary<string, TimeSpan?> _slaThresholds = new();

    /// <summary>
    /// Records a latency measurement for the specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation being measured.</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when operationName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// var sw = Stopwatch.StartNew();
    /// await SearchOperation();
    /// benchmark.RecordLatency("search", sw.Elapsed);
    /// </code>
    /// </example>
    public void RecordLatency(string operationName, TimeSpan duration)
    {
        if (operationName == null)
            throw new ArgumentNullException(nameof(operationName));
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be empty or whitespace.", nameof(operationName));

        if (!_latencies.ContainsKey(operationName))
        {
            _latencies[operationName] = new List<TimeSpan>();
        }

        _latencies[operationName].Add(duration);
    }

    /// <summary>
    /// Calculates percentile statistics for the specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>Percentile statistics including P50, P95, P99, and P100.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operationName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is empty, whitespace, or has no recorded latencies.</exception>
    /// <example>
    /// <code>
    /// var stats = benchmark.GetPercentiles("search");
    /// Console.WriteLine($"P95: {stats.P95.TotalMilliseconds}ms");
    /// </code>
    /// </example>
    public PercentileStats GetPercentiles(string operationName)
    {
        if (operationName == null)
            throw new ArgumentNullException(nameof(operationName));
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be empty or whitespace.", nameof(operationName));
        if (!_latencies.ContainsKey(operationName) || _latencies[operationName].Count == 0)
            throw new ArgumentException($"No latencies recorded for operation: {operationName}", nameof(operationName));

        var sorted = _latencies[operationName].OrderBy(x => x).ToList();
        var count = sorted.Count;

        return new PercentileStats
        {
            P50 = CalculatePercentile(sorted, 50),
            P95 = CalculatePercentile(sorted, 95),
            P99 = CalculatePercentile(sorted, 99),
            P100 = sorted[count - 1],
            SampleCount = count
        };
    }

    /// <summary>
    /// Validates whether an operation meets its SLA threshold based on P95 latency.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="p95Threshold">The maximum acceptable P95 latency.</param>
    /// <returns>True if P95 latency is less than or equal to the threshold; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operationName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is empty, whitespace, or has no recorded latencies.</exception>
    /// <example>
    /// <code>
    /// // Validate that 95% of enrichment operations complete within 200ms
    /// var slaPass = benchmark.ValidateSLA("enrichment", TimeSpan.FromMilliseconds(200));
    /// if (!slaPass)
    /// {
    ///     Console.WriteLine("WARNING: SLA violation detected!");
    /// }
    /// </code>
    /// </example>
    public bool ValidateSLA(string operationName, TimeSpan p95Threshold)
    {
        if (operationName == null)
            throw new ArgumentNullException(nameof(operationName));
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be empty or whitespace.", nameof(operationName));

        var stats = GetPercentiles(operationName);
        var pass = stats.P95 <= p95Threshold;

        // Store threshold for report generation
        _slaThresholds[operationName] = p95Threshold;

        return pass;
    }

    /// <summary>
    /// Generates a comprehensive benchmark report with all operation statistics.
    /// </summary>
    /// <returns>A report containing percentile statistics and SLA validation results for all operations.</returns>
    /// <example>
    /// <code>
    /// var report = benchmark.GenerateReport();
    /// 
    /// // Export as Markdown
    /// File.WriteAllText("benchmark.md", report.ToMarkdown());
    /// 
    /// // Export as JSON
    /// File.WriteAllText("benchmark.json", report.ToJson());
    /// </code>
    /// </example>
    public BenchmarkReport GenerateReport()
    {
        var report = new BenchmarkReport
        {
            GeneratedAt = DateTime.UtcNow
        };

        foreach (var kvp in _latencies.Where(x => x.Value.Count > 0))
        {
            var operationName = kvp.Key;
            var percentiles = GetPercentiles(operationName);
            var threshold = _slaThresholds.ContainsKey(operationName) ? _slaThresholds[operationName] : null;

            report.Operations[operationName] = new OperationStats
            {
                Percentiles = percentiles,
                SlaThreshold = threshold,
                SlaPass = threshold.HasValue && percentiles.P95 <= threshold.Value
            };
        }

        return report;
    }

    private static TimeSpan CalculatePercentile(List<TimeSpan> sorted, double percentile)
    {
        if (sorted.Count == 1)
            return sorted[0];

        // Use linear interpolation for percentile calculation
        var position = (percentile / 100.0) * (sorted.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
            return sorted[lowerIndex];

        // Interpolate between two values
        var fraction = position - lowerIndex;
        var lowerValue = sorted[lowerIndex].TotalMilliseconds;
        var upperValue = sorted[upperIndex].TotalMilliseconds;
        var interpolated = lowerValue + (fraction * (upperValue - lowerValue));

        return TimeSpan.FromMilliseconds(interpolated);
    }
}
