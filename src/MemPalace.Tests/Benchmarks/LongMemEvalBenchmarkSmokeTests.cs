using FluentAssertions;
using MemPalace.Benchmarks.Core;
using MemPalace.Benchmarks.Runners;
using MemPalace.Core.Backends;
using MemPalace.Core.Backends.InMemory;
using MemPalace.Search;
using Microsoft.Extensions.DependencyInjection;

namespace MemPalace.Tests.Benchmarks;

public sealed class LongMemEvalBenchmarkSmokeTests
{
    [Fact]
    public async Task RunAsync_WithSyntheticDataset_CompletesSuccessfully()
    {
        // Find synthetic dataset
        var datasetPath = FindSyntheticDataset("longmemeval.jsonl");
        if (datasetPath == null)
        {
            // Skip test if dataset not found (e.g., running from different directory)
            return;
        }

        var services = BuildServices();
        var ctx = new BenchmarkContext(datasetPath, "./_bench_test", services);

        var benchmark = new LongMemEvalBenchmark();
        var result = await benchmark.RunAsync(ctx);

        result.Should().NotBeNull();
        result.BenchmarkName.Should().Be("longmemeval");
        result.TotalQueries.Should().BeGreaterThan(0);
        result.Recall.Should().BeInRange(0.0, 1.0);
        result.Precision.Should().BeInRange(0.0, 1.0);
        result.F1.Should().BeInRange(0.0, 1.0);
        result.NdcgAt10.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task RunAsync_WithAllBenchmarks_CompletesSuccessfully()
    {
        var benchmarks = new IBenchmark[]
        {
            new LongMemEvalBenchmark(),
            new LoCoMoBenchmark(),
            new ConvoMemBenchmark(),
            new MemBenchBenchmark()
        };

        foreach (var benchmark in benchmarks)
        {
            var datasetPath = FindSyntheticDataset($"{benchmark.Name}.jsonl");
            if (datasetPath == null)
                continue;

            var services = BuildServices();
            var ctx = new BenchmarkContext(datasetPath, $"./_bench_test_{benchmark.Name}", services);

            var result = await benchmark.RunAsync(ctx);

            result.Should().NotBeNull();
            result.BenchmarkName.Should().Be(benchmark.Name);
            result.TotalQueries.Should().BeGreaterThan(0);
        }
    }

    private static string? FindSyntheticDataset(string filename)
    {
        // Try multiple possible locations
        var candidates = new[]
        {
            Path.Combine("datasets-synthetic", filename),
            Path.Combine("src", "MemPalace.Benchmarks", "datasets-synthetic", filename),
            Path.Combine("..", "..", "..", "..", "MemPalace.Benchmarks", "datasets-synthetic", filename)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmbedder>(new DeterministicEmbedder(384));
        services.AddSingleton<IBackend>(new InMemoryBackend());
        services.AddSingleton<ISearchService, VectorSearchService>();
        return services.BuildServiceProvider();
    }
}
