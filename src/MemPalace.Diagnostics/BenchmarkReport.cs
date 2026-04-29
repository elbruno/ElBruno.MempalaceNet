using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemPalace.Diagnostics;

/// <summary>
/// Represents a complete performance benchmark report with all operation statistics.
/// </summary>
public class BenchmarkReport
{
    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of operation statistics keyed by operation name.
    /// </summary>
    public Dictionary<string, OperationStats> Operations { get; set; } = new();

    /// <summary>
    /// Generates a markdown-formatted report.
    /// </summary>
    /// <returns>Markdown table with operation statistics.</returns>
    /// <example>
    /// <code>
    /// var report = benchmark.GenerateReport();
    /// var markdown = report.ToMarkdown();
    /// File.WriteAllText("benchmark.md", markdown);
    /// </code>
    /// </example>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Performance Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("| Operation | Samples | P50 | P95 | P99 | P100 | SLA Status |");
        sb.AppendLine("|-----------|---------|-----|-----|-----|------|------------|");

        foreach (var kvp in Operations.OrderBy(o => o.Key))
        {
            var op = kvp.Value;
            var slaStatus = op.SlaThreshold.HasValue
                ? (op.SlaPass ? "✓ PASS" : "✗ FAIL")
                : "N/A";

            sb.AppendLine($"| {kvp.Key} | {op.Percentiles.SampleCount} | " +
                         $"{FormatTimeSpan(op.Percentiles.P50)} | " +
                         $"{FormatTimeSpan(op.Percentiles.P95)} | " +
                         $"{FormatTimeSpan(op.Percentiles.P99)} | " +
                         $"{FormatTimeSpan(op.Percentiles.P100)} | " +
                         $"{slaStatus} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a JSON-formatted report.
    /// </summary>
    /// <returns>JSON string with operation statistics.</returns>
    /// <example>
    /// <code>
    /// var report = benchmark.GenerateReport();
    /// var json = report.ToJson();
    /// File.WriteAllText("benchmark.json", json);
    /// </code>
    /// </example>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new TimeSpanJsonConverter() }
        };

        return JsonSerializer.Serialize(this, options);
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalMilliseconds < 1)
            return $"{ts.TotalMicroseconds:F0}μs";
        if (ts.TotalSeconds < 1)
            return $"{ts.TotalMilliseconds:F1}ms";
        return $"{ts.TotalSeconds:F2}s";
    }
}

/// <summary>
/// Represents statistics for a single operation type.
/// </summary>
public class OperationStats
{
    /// <summary>
    /// Gets or sets the percentile statistics for this operation.
    /// </summary>
    public PercentileStats Percentiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the SLA threshold (P95), if configured.
    /// </summary>
    public TimeSpan? SlaThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether the operation passed SLA validation.
    /// </summary>
    public bool SlaPass { get; set; }
}

/// <summary>
/// Custom JSON converter for TimeSpan to output milliseconds as a number.
/// </summary>
internal class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TimeSpan.FromMilliseconds(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.TotalMilliseconds);
    }
}
