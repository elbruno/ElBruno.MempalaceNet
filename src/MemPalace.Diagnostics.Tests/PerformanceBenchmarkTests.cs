using FluentAssertions;
using MemPalace.Diagnostics;

namespace MemPalace.Tests.Diagnostics;

public class PerformanceBenchmarkTests
{
    [Fact]
    public void RecordLatency_WithValidInput_StoresLatency()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        benchmark.RecordLatency("test-op", duration);
        var stats = benchmark.GetPercentiles("test-op");

        // Assert
        stats.SampleCount.Should().Be(1);
        stats.P50.Should().Be(duration);
    }

    [Fact]
    public void RecordLatency_WithNullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var act = () => benchmark.RecordLatency(null!, TimeSpan.FromMilliseconds(100));

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operationName");
    }

    [Fact]
    public void RecordLatency_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var act = () => benchmark.RecordLatency("", TimeSpan.FromMilliseconds(100));

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("operationName");
    }

    [Fact]
    public void RecordLatency_WithWhitespaceOperationName_ThrowsArgumentException()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var act = () => benchmark.RecordLatency("   ", TimeSpan.FromMilliseconds(100));

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("operationName");
    }

    [Fact]
    public void GetPercentiles_WithNoRecordedLatencies_ThrowsArgumentException()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var act = () => benchmark.GetPercentiles("nonexistent");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No latencies recorded*");
    }

    [Fact]
    public void GetPercentiles_WithSingleSample_ReturnsAllPercentilesEqual()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        var duration = TimeSpan.FromMilliseconds(100);
        benchmark.RecordLatency("test-op", duration);

        // Act
        var stats = benchmark.GetPercentiles("test-op");

        // Assert
        stats.P50.Should().Be(duration);
        stats.P95.Should().Be(duration);
        stats.P99.Should().Be(duration);
        stats.P100.Should().Be(duration);
        stats.SampleCount.Should().Be(1);
    }

    [Fact]
    public void GetPercentiles_WithIdenticalSamples_ReturnsAllPercentilesEqual()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        var duration = TimeSpan.FromMilliseconds(100);
        
        for (int i = 0; i < 10; i++)
        {
            benchmark.RecordLatency("test-op", duration);
        }

        // Act
        var stats = benchmark.GetPercentiles("test-op");

        // Assert
        stats.P50.Should().Be(duration);
        stats.P95.Should().Be(duration);
        stats.P99.Should().Be(duration);
        stats.P100.Should().Be(duration);
        stats.SampleCount.Should().Be(10);
    }

    [Fact]
    public void GetPercentiles_WithMultipleSamples_CalculatesCorrectPercentiles()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        // Record 100 samples: 1ms, 2ms, ..., 100ms
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("test-op", TimeSpan.FromMilliseconds(i));
        }

        // Act
        var stats = benchmark.GetPercentiles("test-op");

        // Assert
        stats.SampleCount.Should().Be(100);
        stats.P50.TotalMilliseconds.Should().BeApproximately(50.5, 0.5); // Median
        stats.P95.TotalMilliseconds.Should().BeApproximately(95.05, 0.5); // 95th percentile
        stats.P99.TotalMilliseconds.Should().BeApproximately(99.01, 0.5); // 99th percentile
        stats.P100.Should().Be(TimeSpan.FromMilliseconds(100)); // Max
    }

    [Fact]
    public void GetPercentiles_WithTwoSamples_InterpolatesCorrectly()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        benchmark.RecordLatency("test-op", TimeSpan.FromMilliseconds(10));
        benchmark.RecordLatency("test-op", TimeSpan.FromMilliseconds(20));

        // Act
        var stats = benchmark.GetPercentiles("test-op");

        // Assert
        stats.P50.TotalMilliseconds.Should().BeApproximately(15, 0.1); // Interpolated median
        stats.P100.Should().Be(TimeSpan.FromMilliseconds(20)); // Max
    }

    [Fact]
    public void ValidateSLA_WithPassingThreshold_ReturnsTrue()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("enrichment", TimeSpan.FromMilliseconds(i));
        }

        // Act
        var result = benchmark.ValidateSLA("enrichment", TimeSpan.FromMilliseconds(200));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSLA_WithFailingThreshold_ReturnsFalse()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("enrichment", TimeSpan.FromMilliseconds(i));
        }

        // Act
        var result = benchmark.ValidateSLA("enrichment", TimeSpan.FromMilliseconds(50));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSLA_WithExactThreshold_ReturnsTrue()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("enrichment", TimeSpan.FromMilliseconds(i));
        }

        var stats = benchmark.GetPercentiles("enrichment");

        // Act
        var result = benchmark.ValidateSLA("enrichment", stats.P95);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GenerateReport_WithNoOperations_ReturnsEmptyReport()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var report = benchmark.GenerateReport();

        // Assert
        report.Operations.Should().BeEmpty();
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateReport_WithMultipleOperations_IncludesAllOperations()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        benchmark.RecordLatency("op1", TimeSpan.FromMilliseconds(50));
        benchmark.RecordLatency("op2", TimeSpan.FromMilliseconds(100));
        benchmark.RecordLatency("op3", TimeSpan.FromMilliseconds(150));

        // Act
        var report = benchmark.GenerateReport();

        // Assert
        report.Operations.Should().HaveCount(3);
        report.Operations.Should().ContainKey("op1");
        report.Operations.Should().ContainKey("op2");
        report.Operations.Should().ContainKey("op3");
    }

    [Fact]
    public void GenerateReport_WithSLAValidation_IncludesSLAStatus()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("passing", TimeSpan.FromMilliseconds(i));
            benchmark.RecordLatency("failing", TimeSpan.FromMilliseconds(i * 2));
        }

        benchmark.ValidateSLA("passing", TimeSpan.FromMilliseconds(200));
        benchmark.ValidateSLA("failing", TimeSpan.FromMilliseconds(100));

        // Act
        var report = benchmark.GenerateReport();

        // Assert
        report.Operations["passing"].SlaPass.Should().BeTrue();
        report.Operations["passing"].SlaThreshold.Should().Be(TimeSpan.FromMilliseconds(200));
        
        report.Operations["failing"].SlaPass.Should().BeFalse();
        report.Operations["failing"].SlaThreshold.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void BenchmarkReport_ToMarkdown_GeneratesValidMarkdown()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 10; i++)
        {
            benchmark.RecordLatency("test-op", TimeSpan.FromMilliseconds(i * 10));
        }

        benchmark.ValidateSLA("test-op", TimeSpan.FromMilliseconds(200));

        // Act
        var report = benchmark.GenerateReport();
        var markdown = report.ToMarkdown();

        // Assert
        markdown.Should().Contain("# Performance Benchmark Report");
        markdown.Should().Contain("| Operation | Samples | P50 | P95 | P99 | P100 | SLA Status |");
        markdown.Should().Contain("test-op");
        markdown.Should().Contain("✓ PASS");
        markdown.Should().Contain("10"); // sample count
    }

    [Fact]
    public void BenchmarkReport_ToJson_GeneratesValidJson()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 10; i++)
        {
            benchmark.RecordLatency("test-op", TimeSpan.FromMilliseconds(i * 10));
        }

        // Act
        var report = benchmark.GenerateReport();
        var json = report.ToJson();

        // Assert
        json.Should().Contain("\"GeneratedAt\"");
        json.Should().Contain("\"Operations\"");
        json.Should().Contain("\"test-op\"");
        json.Should().Contain("\"Percentiles\"");
        json.Should().Contain("\"SampleCount\"");
    }

    [Fact]
    public void RecordLatency_WithMultipleOperations_IsolatesOperations()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        benchmark.RecordLatency("op1", TimeSpan.FromMilliseconds(10));
        benchmark.RecordLatency("op2", TimeSpan.FromMilliseconds(20));

        // Act
        var stats1 = benchmark.GetPercentiles("op1");
        var stats2 = benchmark.GetPercentiles("op2");

        // Assert
        stats1.P50.Should().Be(TimeSpan.FromMilliseconds(10));
        stats2.P50.Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public void GetPercentiles_WithLargeDataset_PerformsCorrectly()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        var random = new Random(42);
        
        for (int i = 0; i < 10000; i++)
        {
            benchmark.RecordLatency("large-op", TimeSpan.FromMilliseconds(random.Next(1, 1000)));
        }

        // Act
        var stats = benchmark.GetPercentiles("large-op");

        // Assert
        stats.SampleCount.Should().Be(10000);
        stats.P50.Should().BeLessThan(stats.P95);
        stats.P95.Should().BeLessThan(stats.P99);
        stats.P99.Should().BeLessThanOrEqualTo(stats.P100);
    }

    [Fact]
    public void BenchmarkReport_ToMarkdown_WithFailingSLA_ShowsFailStatus()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("slow-op", TimeSpan.FromMilliseconds(i * 2));
        }

        benchmark.ValidateSLA("slow-op", TimeSpan.FromMilliseconds(50));

        // Act
        var report = benchmark.GenerateReport();
        var markdown = report.ToMarkdown();

        // Assert
        markdown.Should().Contain("✗ FAIL");
    }

    [Fact]
    public void BenchmarkReport_ToMarkdown_WithNoSLA_ShowsNA()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        benchmark.RecordLatency("no-sla-op", TimeSpan.FromMilliseconds(50));

        // Act
        var report = benchmark.GenerateReport();
        var markdown = report.ToMarkdown();

        // Assert
        markdown.Should().Contain("N/A");
    }

    [Fact]
    public void ValidateSLAs_WithAllPassing_ReturnsSuccess()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("op1", TimeSpan.FromMilliseconds(i));
            benchmark.RecordLatency("op2", TimeSpan.FromMilliseconds(i * 0.5));
        }

        var thresholds = new Dictionary<string, TimeSpan>
        {
            { "op1", TimeSpan.FromMilliseconds(200) },
            { "op2", TimeSpan.FromMilliseconds(100) }
        };

        // Act
        var result = benchmark.ValidateSLAs(thresholds);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.OperationResults["op1"].Should().BeTrue();
        result.OperationResults["op2"].Should().BeTrue();
    }

    [Fact]
    public void ValidateSLAs_WithSomeFailures_ReturnsFailure()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        
        for (int i = 1; i <= 100; i++)
        {
            benchmark.RecordLatency("fast-op", TimeSpan.FromMilliseconds(i));
            benchmark.RecordLatency("slow-op", TimeSpan.FromMilliseconds(i * 3));
        }

        var thresholds = new Dictionary<string, TimeSpan>
        {
            { "fast-op", TimeSpan.FromMilliseconds(200) },
            { "slow-op", TimeSpan.FromMilliseconds(100) }
        };

        // Act
        var result = benchmark.ValidateSLAs(thresholds);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("slow-op");
        result.Errors[0].Should().Contain("failed SLA");
        result.OperationResults["fast-op"].Should().BeTrue();
        result.OperationResults["slow-op"].Should().BeFalse();
    }

    [Fact]
    public void ValidateSLAs_WithNonexistentOperation_ReturnsFailure()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();
        benchmark.RecordLatency("existing-op", TimeSpan.FromMilliseconds(50));

        var thresholds = new Dictionary<string, TimeSpan>
        {
            { "existing-op", TimeSpan.FromMilliseconds(100) },
            { "nonexistent-op", TimeSpan.FromMilliseconds(100) }
        };

        // Act
        var result = benchmark.ValidateSLAs(thresholds);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
        result.Errors.Should().Contain(e => e.Contains("nonexistent-op"));
        result.OperationResults["existing-op"].Should().BeTrue();
        result.OperationResults["nonexistent-op"].Should().BeFalse();
    }

    [Fact]
    public void ValidateSLAs_WithNullThresholds_ThrowsArgumentNullException()
    {
        // Arrange
        var benchmark = new PerformanceBenchmark();

        // Act
        var act = () => benchmark.ValidateSLAs(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("thresholds");
    }

    [Fact]
    public void ValidationResult_Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_CreatesInvalidResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }
}
