using MemPalace.Benchmarks.Core;
using MemPalace.Benchmarks.Scoring;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using Microsoft.Extensions.DependencyInjection;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// LongMemEval benchmark: long-conversation memory recall.
/// Dataset expects conversational turns with memory IDs.
/// </summary>
public sealed class LongMemEvalBenchmark : BenchmarkBase
{
    public override string Name => "longmemeval";
    public override string Description => "Long-conversation memory recall (LongMemEval)";

    protected override async Task<BenchmarkResult> RunLoadedAsync(
        BenchmarkContext ctx,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        if (!items.Any(item => item.CorpusDocuments is { Count: > 0 }))
            return await base.RunLoadedAsync(ctx, items, ct);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var backend = ctx.Services.GetRequiredService<IBackend>();
        var embedder = ctx.Services.GetRequiredService<IEmbedder>();
        var palace = CreatePalace(ctx);
        var queryResults = new List<(DatasetItem Item, IReadOnlyList<string> Retrieved)>();
        var recallAt5 = 0.0;
        var recallAt10 = 0.0;
        var recallAllAt5 = 0.0;
        var recallAllAt10 = 0.0;
        var precisionAt10 = 0.0;
        var ndcgAt10 = 0.0;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Question) || item.CorpusDocuments is not { Count: > 0 } corpus)
                continue;

            await PrepareCollectionAsync(backend, palace, ct);
            await using var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: true, embedder, ct);
            {
                if (corpus.Count == 0)
                    continue;

                var texts = corpus.Select(document => document.Document).ToList();
                var embeddings = await embedder.EmbedAsync(texts, ct);
                var records = new List<EmbeddedRecord>(corpus.Count);

                for (var i = 0; i < corpus.Count; i++)
                {
                    records.Add(new EmbeddedRecord(
                        Id: corpus[i].Id,
                        Document: corpus[i].Document,
                        Metadata: corpus[i].Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                        Embedding: embeddings[i]));
                }

                await collection.UpsertAsync(records, ct);

                var queryEmbeddings = await embedder.EmbedAsync(new[] { item.Question }, ct);
                var queryResult = await collection.QueryAsync(
                    queryEmbeddings,
                    nResults: Math.Min(50, records.Count),
                    include: IncludeFields.Distances,
                    ct: ct);

                var retrieved = queryResult.Ids.Count > 0
                    ? queryResult.Ids[0].ToList()
                    : new List<string>();

                queryResults.Add((item, retrieved));

                recallAt5 += AnyRecall(retrieved, item.RelevantMemoryIds, 5);
                recallAt10 += AnyRecall(retrieved, item.RelevantMemoryIds, 10);
                recallAllAt5 += AllRecall(retrieved, item.RelevantMemoryIds, 5);
                recallAllAt10 += AllRecall(retrieved, item.RelevantMemoryIds, 10);
                precisionAt10 += Metrics.Precision(retrieved, item.RelevantMemoryIds, 10);
                ndcgAt10 += Metrics.NdcgAtK(retrieved, item.RelevantMemoryIds, 10);
            }
        }

        stopwatch.Stop();

        var totalQueries = queryResults.Count;
        var avgRecallAt10 = totalQueries > 0 ? recallAt10 / totalQueries : 0.0;
        var avgPrecisionAt10 = totalQueries > 0 ? precisionAt10 / totalQueries : 0.0;
        var result = new BenchmarkResult(
            BenchmarkName: Name,
            TotalQueries: totalQueries,
            Correct: queryResults.Count(result => AnyRecall(result.Retrieved, result.Item.RelevantMemoryIds, 10) > 0),
            Recall: avgRecallAt10,
            Precision: avgPrecisionAt10,
            F1: Metrics.F1(avgPrecisionAt10, avgRecallAt10),
            NdcgAt10: totalQueries > 0 ? ndcgAt10 / totalQueries : 0.0,
            TotalDuration: stopwatch.Elapsed,
            ExtraMetrics: new Dictionary<string, double>
            {
                ["Recall@5"] = totalQueries > 0 ? recallAt5 / totalQueries : 0.0,
                ["Recall@10"] = avgRecallAt10,
                ["RecallAll@5"] = totalQueries > 0 ? recallAllAt5 / totalQueries : 0.0,
                ["RecallAll@10"] = totalQueries > 0 ? recallAllAt10 / totalQueries : 0.0,
                ["CorpusSizeAvg"] = totalQueries > 0
                    ? queryResults
                        .Where(result => result.Item.CorpusDocuments is { Count: > 0 })
                        .Average(result => result.Item.CorpusDocuments!.Count)
                    : 0.0
            });

        return result;
    }

    protected override async Task IngestMemoriesAsync(
        IBackend backend,
        PalaceRef palace,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: true, embedder, ct);

        // Group by session and ingest conversation memories
        var sessions = items
            .Where(item => item.Metadata.TryGetValue("session_id", out var sid) && sid != null)
            .GroupBy(item => item.Metadata["session_id"]?.ToString() ?? "default");

        foreach (var session in sessions)
        {
            var sessionItems = session
                .Where(item => item.Metadata.ContainsKey("turn") && !string.IsNullOrWhiteSpace(item.ExpectedAnswer))
                .OrderBy(item => Convert.ToInt32(item.Metadata["turn"]))
                .ToList();

            var records = new List<EmbeddedRecord>();
            var texts = sessionItems.Select(item => item.ExpectedAnswer).ToList();
            var embeddings = await embedder.EmbedAsync(texts, ct);

            for (var i = 0; i < sessionItems.Count; i++)
            {
                var item = sessionItems[i];
                var memId = item.RelevantMemoryIds.FirstOrDefault() ?? item.Id;
                
                records.Add(new EmbeddedRecord(
                    Id: memId,
                    Document: item.ExpectedAnswer,
                    Metadata: item.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Embedding: embeddings[i]));
            }

            if (records.Count > 0)
                await collection.UpsertAsync(records, ct);
        }
    }

    private static double AnyRecall(IReadOnlyList<string> retrieved, IReadOnlyList<string> relevant, int k)
    {
        if (relevant.Count == 0)
            return 0.0;

        var topK = retrieved.Take(k).ToHashSet();
        return relevant.Any(id => topK.Contains(id)) ? 1.0 : 0.0;
    }

    private static double AllRecall(IReadOnlyList<string> retrieved, IReadOnlyList<string> relevant, int k)
    {
        if (relevant.Count == 0)
            return 0.0;

        var topK = retrieved.Take(k).ToHashSet();
        return relevant.All(id => topK.Contains(id)) ? 1.0 : 0.0;
    }
}
