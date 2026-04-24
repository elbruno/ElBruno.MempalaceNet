using System.Diagnostics;
using MemPalace.Benchmarks.Core;
using MemPalace.Benchmarks.Scoring;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Mining;
using MemPalace.Search;
using Microsoft.Extensions.DependencyInjection;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// Base class for benchmark runners with common logic.
/// </summary>
public abstract class BenchmarkBase : IBenchmark
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    protected const string DefaultCollection = "benchmark";
    protected const int DefaultTopK = 10;

    public async Task<BenchmarkResult> RunAsync(BenchmarkContext ctx, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Load dataset
        var items = await DatasetLoader.LoadAsync(ctx.DatasetPath, ctx.MaxItems, ct).ToListAsync(ct);
        
        // Get services
        var backend = ctx.Services.GetRequiredService<IBackend>();
        var embedder = ctx.Services.GetRequiredService<IEmbedder>();
        var searchService = ctx.Services.GetRequiredService<ISearchService>();

        // Prepare collection
        await PrepareCollectionAsync(backend, embedder, items, ct);

        // Ingest memories
        await IngestMemoriesAsync(backend, embedder, items, ct);

        // Run queries
        var queryResults = new List<(DatasetItem Item, IReadOnlyList<string> Retrieved)>();
        
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Question))
                continue;

            var opts = new SearchOptions(TopK: DefaultTopK);
            var hits = await searchService.SearchAsync(item.Question, DefaultCollection, opts, ct);
            var retrievedIds = hits.Select(h => h.Id).ToList();
            queryResults.Add((item, retrievedIds));
        }

        // Compute metrics
        var totalQueries = queryResults.Count;
        var correct = 0;
        var sumRecall = 0.0;
        var sumPrecision = 0.0;
        var sumNdcg = 0.0;

        foreach (var (item, retrieved) in queryResults)
        {
            var recall = Metrics.Recall(retrieved, item.RelevantMemoryIds, DefaultTopK);
            var precision = Metrics.Precision(retrieved, item.RelevantMemoryIds, DefaultTopK);
            var ndcg = Metrics.NdcgAtK(retrieved, item.RelevantMemoryIds, DefaultTopK);

            if (recall > 0)
                correct++;

            sumRecall += recall;
            sumPrecision += precision;
            sumNdcg += ndcg;
        }

        var avgRecall = totalQueries > 0 ? sumRecall / totalQueries : 0.0;
        var avgPrecision = totalQueries > 0 ? sumPrecision / totalQueries : 0.0;
        var avgNdcg = totalQueries > 0 ? sumNdcg / totalQueries : 0.0;
        var f1 = Metrics.F1(avgPrecision, avgRecall);

        stopwatch.Stop();

        var extraMetrics = ComputeExtraMetrics(items, queryResults);

        return new BenchmarkResult(
            BenchmarkName: Name,
            TotalQueries: totalQueries,
            Correct: correct,
            Recall: avgRecall,
            Precision: avgPrecision,
            F1: f1,
            NdcgAt10: avgNdcg,
            TotalDuration: stopwatch.Elapsed,
            ExtraMetrics: extraMetrics);
    }

    protected virtual async Task PrepareCollectionAsync(
        IBackend backend,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        // Just create the collection - no need to drop since each run uses unique palace
        var palace = new PalaceRef(
            Id: Guid.NewGuid().ToString(),
            LocalPath: Environment.CurrentDirectory,
            Namespace: "benchmark");
    }

    protected abstract Task IngestMemoriesAsync(
        IBackend backend,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct);

    protected virtual IReadOnlyDictionary<string, double> ComputeExtraMetrics(
        IReadOnlyList<DatasetItem> items,
        IReadOnlyList<(DatasetItem Item, IReadOnlyList<string> Retrieved)> queryResults)
    {
        return new Dictionary<string, double>();
    }
}
