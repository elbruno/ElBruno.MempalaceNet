namespace MemPalace.Diagnostics;

/// <summary>
/// Represents percentile statistics for latency measurements.
/// </summary>
/// <remarks>
/// Percentiles indicate the value below which a given percentage of observations fall.
/// For example, P95 of 100ms means 95% of operations completed in 100ms or less.
/// </remarks>
public class PercentileStats
{
    /// <summary>
    /// Gets or sets the 50th percentile (median) latency.
    /// </summary>
    public TimeSpan P50 { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile latency.
    /// </summary>
    /// <remarks>
    /// Commonly used for SLA validation. Represents the upper bound for 95% of requests.
    /// </remarks>
    public TimeSpan P95 { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile latency.
    /// </summary>
    /// <remarks>
    /// Captures near-worst-case performance, excluding extreme outliers.
    /// </remarks>
    public TimeSpan P99 { get; set; }

    /// <summary>
    /// Gets or sets the 100th percentile (maximum) latency.
    /// </summary>
    public TimeSpan P100 { get; set; }

    /// <summary>
    /// Gets or sets the number of samples in the dataset.
    /// </summary>
    public int SampleCount { get; set; }
}
