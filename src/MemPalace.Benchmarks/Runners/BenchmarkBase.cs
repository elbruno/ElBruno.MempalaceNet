using System.Diagnostics;
using MemPalace.Benchmarks.Core;
using MemPalace.Benchmarks.Scoring;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
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

    public virtual async Task<BenchmarkResult> RunAsync(BenchmarkContext ctx, CancellationToken ct = default)
    {
        var items = await DatasetLoader.LoadAsync(ctx.DatasetPath, ctx.MaxItems, ct).ToListAsync(ct);
        return await RunLoadedAsync(ctx, items, ct);
    }

    protected virtual async Task<BenchmarkResult> RunLoadedAsync(
        BenchmarkContext ctx,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get services
        var backend = ctx.Services.GetRequiredService<IBackend>();
        var embedder = ctx.Services.GetRequiredService<IEmbedder>();
        var palace = CreatePalace(ctx);

        // Prepare collection
        await PrepareCollectionAsync(backend, palace, ct);

        // Ingest memories
        await IngestMemoriesAsync(backend, palace, embedder, items, ct);

        // Run queries
        var queryResults = new List<(DatasetItem Item, IReadOnlyList<string> Retrieved)>();
        await using var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: false, embedder, ct);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Question))
                continue;

            var queryEmbeddings = await embedder.EmbedAsync(new[] { item.Question }, ct);
            var result = await collection.QueryAsync(
                queryEmbeddings,
                nResults: DefaultTopK,
                include: IncludeFields.Distances,
                ct: ct);

            var retrievedIds = result.Ids.Count > 0
                ? result.Ids[0].ToList()
                : new List<string>();

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

    protected virtual PalaceRef CreatePalace(BenchmarkContext ctx)
    {
        return new PalaceRef(
            Id: $"{Name}-benchmark",
            LocalPath: Path.GetFullPath(ctx.PalacePath),
            Namespace: "benchmark");
    }

    protected virtual async Task PrepareCollectionAsync(
        IBackend backend,
        PalaceRef palace,
        CancellationToken ct)
    {
        try
        {
            await backend.DeleteCollectionAsync(palace, DefaultCollection, ct);
        }
        catch
        {
            // Fresh run path; nothing to delete.
        }
    }

    protected abstract Task IngestMemoriesAsync(
        IBackend backend,
        PalaceRef palace,
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
