# Skill: Performance Tracking with MemPalace.Diagnostics

**Status:** Active  
**Domain:** Diagnostics, SLA Validation, Benchmarking  
**Created:** 2026-04-28  
**Author:** Rachael (CLI/UX Dev)

---

## Overview

Use `MemPalace.Diagnostics.PerformanceBenchmark` to track operation latencies, calculate percentile statistics (P50/P95/P99/P100), validate SLA compliance, and generate human/machine-readable reports.

---

## When to Use This Skill

- ✅ Tracking latency of API calls, database queries, AI inference, search operations
- ✅ Validating P95/P99 SLA compliance in integration tests or CI pipelines
- ✅ Generating performance reports for trend analysis or debugging
- ✅ Comparing performance across different implementations (e.g., A/B testing)
- ✅ Monitoring long-running services with periodic latency snapshots

❌ **Do NOT use for:**
- Real-time metrics collection in production (use OpenTelemetry, Prometheus instead)
- Memory profiling or CPU profiling (use dotnet-trace, PerfView instead)
- Distributed tracing (use Application Insights, Jaeger instead)

---

## Pattern 1: Basic Latency Tracking

**Use Case:** Measure and validate a single operation's P95 latency.

```csharp
using MemPalace.Diagnostics;
using System.Diagnostics;

[Fact]
public async Task SearchOperation_MeetsP95SLA()
{
    var benchmark = new PerformanceBenchmark();
    
    // Run 100 search operations
    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        await _searchService.Search("test query");
        benchmark.RecordLatency("search", sw.Elapsed);
    }
    
    // Validate P95 < 100ms
    var passes = benchmark.ValidateSLA("search", TimeSpan.FromMilliseconds(100));
    
    if (!passes)
    {
        var stats = benchmark.GetPercentiles("search");
        Assert.Fail($"Search P95 SLA failed: {stats.P95.TotalMilliseconds:F2}ms exceeds 100ms");
    }
}
```

---

## Pattern 2: Multi-Operation Batch Validation

**Use Case:** Validate multiple operations against their SLAs in a single test.

```csharp
[Fact]
public async Task HybridSearchPipeline_MeetsAllSLAs()
{
    var benchmark = new PerformanceBenchmark();
    
    // Simulate 100 requests through the pipeline
    for (int i = 0; i < 100; i++)
    {
        // Semantic search
        var sw1 = Stopwatch.StartNew();
        await _semanticSearch.Search("query");
        benchmark.RecordLatency("semantic", sw1.Elapsed);
        
        // Reranking
        var sw2 = Stopwatch.StartNew();
        await _reranker.Rerank(results);
        benchmark.RecordLatency("rerank", sw2.Elapsed);
        
        // Total pipeline
        var sw3 = Stopwatch.StartNew();
        await _pipeline.Execute("query");
        benchmark.RecordLatency("total", sw3.Elapsed);
    }
    
    // Define SLA thresholds
    var slas = new Dictionary<string, TimeSpan>
    {
        { "semantic", TimeSpan.FromMilliseconds(50) },   // <50ms P95
        { "rerank", TimeSpan.FromMilliseconds(100) },    // <100ms P95
        { "total", TimeSpan.FromMilliseconds(200) }      // <200ms P95
    };
    
    // Validate all SLAs
    var result = benchmark.ValidateSLAs(slas);
    
    if (!result.IsValid)
    {
        var report = benchmark.GenerateReport();
        Console.WriteLine(report.ToMarkdown());
        Assert.Fail($"SLA violations detected:\n{string.Join("\n", result.Errors)}");
    }
}
```

---

## Pattern 3: Report Generation for CI

**Use Case:** Generate performance reports in CI pipelines for trend tracking.

```csharp
[Fact]
public async Task GeneratePerformanceReport_ForCI()
{
    var benchmark = new PerformanceBenchmark();
    
    // Run benchmarks
    await RunBenchmarkSuite(benchmark);
    
    // Generate report
    var report = benchmark.GenerateReport();
    
    // Write markdown to file (for GitHub Actions summary)
    File.WriteAllText("benchmark-report.md", report.ToMarkdown());
    
    // Write JSON to file (for dashboards/charts)
    File.WriteAllText("benchmark-report.json", report.ToJson());
    
    Console.WriteLine("Performance report generated successfully");
}
```

**GitHub Actions Integration:**

```yaml
- name: Run Performance Tests
  run: dotnet test --filter Category=Performance
  
- name: Upload Benchmark Report
  run: cat benchmark-report.md >> $GITHUB_STEP_SUMMARY
  
- name: Archive JSON Results
  uses: actions/upload-artifact@v4
  with:
    name: benchmark-results
    path: benchmark-report.json
```

---

## Pattern 4: Edge Case Detection

**Use Case:** Detect outliers and worst-case latencies.

```csharp
[Fact]
public async Task DetectPerformanceOutliers()
{
    var benchmark = new PerformanceBenchmark();
    
    // Record 1000 samples
    for (int i = 0; i < 1000; i++)
    {
        var sw = Stopwatch.StartNew();
        await _service.Process(testData[i]);
        benchmark.RecordLatency("process", sw.Elapsed);
    }
    
    var stats = benchmark.GetPercentiles("process");
    
    // Check for outliers (P99 significantly higher than P95)
    var p95ToP99Ratio = stats.P99.TotalMilliseconds / stats.P95.TotalMilliseconds;
    if (p95ToP99Ratio > 2.0)
    {
        Console.WriteLine($"WARNING: P99 outliers detected!");
        Console.WriteLine($"P95: {stats.P95.TotalMilliseconds:F2}ms");
        Console.WriteLine($"P99: {stats.P99.TotalMilliseconds:F2}ms");
        Console.WriteLine($"P100: {stats.P100.TotalMilliseconds:F2}ms");
    }
    
    // Validate worst-case latency
    Assert.True(stats.P100 < TimeSpan.FromSeconds(1), 
        $"Worst-case latency {stats.P100.TotalMilliseconds}ms exceeds 1000ms");
}
```

---

## Pattern 5: A/B Performance Comparison

**Use Case:** Compare two implementations and pick the faster one.

```csharp
[Theory]
[InlineData("implementation-a")]
[InlineData("implementation-b")]
public async Task CompareImplementations(string implementation)
{
    var benchmark = new PerformanceBenchmark();
    var service = CreateService(implementation);
    
    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        await service.Process(testData);
        benchmark.RecordLatency(implementation, sw.Elapsed);
    }
    
    var stats = benchmark.GetPercentiles(implementation);
    Console.WriteLine($"{implementation} P95: {stats.P95.TotalMilliseconds:F2}ms");
    
    // Store results for comparison
    _testResults[implementation] = stats;
}

[Fact]
public void SelectFasterImplementation()
{
    var statsA = _testResults["implementation-a"];
    var statsB = _testResults["implementation-b"];
    
    var winner = statsA.P95 < statsB.P95 ? "implementation-a" : "implementation-b";
    Console.WriteLine($"Winner: {winner} (P95: {Math.Min(statsA.P95, statsB.P95).TotalMilliseconds:F2}ms)");
    
    // Optionally generate comparison report
    var benchmark = new PerformanceBenchmark();
    // ... (reconstruct from stored results or re-run)
    var report = benchmark.GenerateReport();
    Console.WriteLine(report.ToMarkdown());
}
```

---

## Pattern 6: Real-World Load Simulation

**Use Case:** Simulate realistic load patterns with varying latencies.

```csharp
[Fact]
public async Task SimulateRealisticLoad()
{
    var benchmark = new PerformanceBenchmark();
    var random = new Random(42);
    
    // Simulate 10,000 requests with realistic distribution
    for (int i = 0; i < 10_000; i++)
    {
        // 80% fast requests, 15% medium, 5% slow
        var requestType = random.NextDouble();
        var testCase = requestType switch
        {
            < 0.80 => "fast-query",    // <50ms
            < 0.95 => "medium-query",  // 50-200ms
            _ => "slow-query"          // 200-1000ms
        };
        
        var sw = Stopwatch.StartNew();
        await _service.Query(testCase);
        benchmark.RecordLatency("mixed-load", sw.Elapsed);
    }
    
    var stats = benchmark.GetPercentiles("mixed-load");
    
    // Validate realistic SLA (P95 should be ~200ms based on distribution)
    Assert.True(stats.P95 < TimeSpan.FromMilliseconds(250), 
        $"P95 latency {stats.P95.TotalMilliseconds}ms exceeds expected 250ms for mixed load");
    
    Console.WriteLine($"P50: {stats.P50.TotalMilliseconds:F2}ms");
    Console.WriteLine($"P95: {stats.P95.TotalMilliseconds:F2}ms");
    Console.WriteLine($"P99: {stats.P99.TotalMilliseconds:F2}ms");
}
```

---

## Pattern 7: Incremental Benchmark (Long-Running Services)

**Use Case:** Track performance over time in a long-running service.

```csharp
public class PerformanceMonitor
{
    private readonly PerformanceBenchmark _benchmark = new();
    private readonly Timer _reportTimer;
    
    public PerformanceMonitor()
    {
        // Generate report every 5 minutes
        _reportTimer = new Timer(GenerateReport, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public void RecordOperation(string operationName, TimeSpan duration)
    {
        _benchmark.RecordLatency(operationName, duration);
    }
    
    private void GenerateReport(object? state)
    {
        var report = _benchmark.GenerateReport();
        
        // Log to file
        File.AppendAllText("performance.log", 
            $"\n=== {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===\n{report.ToMarkdown()}");
        
        // Send to monitoring service
        _monitoringClient.SendMetrics(report.ToJson());
        
        // Optional: Reset benchmark for next interval
        _benchmark = new PerformanceBenchmark();
    }
}
```

---

## Markdown Report Example

```markdown
# Performance Benchmark Report

Generated: 2026-04-28 15:30:45 UTC

| Operation | Samples | P50 | P95 | P99 | P100 | SLA Status |
|-----------|---------|-----|-----|-----|------|------------|
| semantic | 100 | 35.2ms | 48.5ms | 62.1ms | 85.3ms | ✓ PASS |
| rerank | 100 | 78.4ms | 95.2ms | 112.8ms | 145.7ms | ✓ PASS |
| total | 100 | 125.6ms | 178.3ms | 205.4ms | 245.9ms | ✗ FAIL |
```

---

## JSON Report Example

```json
{
  "GeneratedAt": "2026-04-28T15:30:45.123Z",
  "Operations": {
    "semantic": {
      "Percentiles": {
        "P50": 35.2,
        "P95": 48.5,
        "P99": 62.1,
        "P100": 85.3,
        "SampleCount": 100
      },
      "SlaThreshold": 50.0,
      "SlaPass": true
    },
    "rerank": {
      "Percentiles": {
        "P50": 78.4,
        "P95": 95.2,
        "P99": 112.8,
        "P100": 145.7,
        "SampleCount": 100
      },
      "SlaThreshold": 100.0,
      "SlaPass": true
    }
  }
}
```

---

## Common Pitfalls

1. **Too Few Samples:**
   - ❌ 10 samples → unstable percentiles
   - ✅ 100+ samples → reliable P95/P99

2. **Ignoring Warmup:**
   - ❌ Including JIT/cold-start latencies
   - ✅ Discard first N samples or run warmup phase

3. **Blocking I/O in Async:**
   - ❌ `.Result` or `.Wait()` → thread starvation
   - ✅ `await` all async operations

4. **Mixing Operation Types:**
   - ❌ Recording "api-call" for both fast + slow endpoints
   - ✅ Separate operation names: "api-search", "api-export"

5. **SLA Thresholds Too Tight:**
   - ❌ P95 < 50ms for disk-bound operations
   - ✅ Analyze current P95, set realistic thresholds

---

## References

- Implementation: `src/MemPalace.Diagnostics/PerformanceBenchmark.cs`
- Tests: `src/MemPalace.Diagnostics.Tests/PerformanceBenchmarkTests.cs`
- OpenClawNet SLAs: GitHub Issue #24
- Related: `docs/DIAGNOSTICS.md` (future)

---

## Related Skills

- **[load-testing]**: Combine with k6, JMeter, or BenchmarkDotNet for comprehensive performance testing
- **[monitoring-integration]**: Export JSON to Prometheus, Grafana, or Application Insights
- **[ci-benchmarks]**: Integrate with GitHub Actions, Azure Pipelines for automated performance tracking
