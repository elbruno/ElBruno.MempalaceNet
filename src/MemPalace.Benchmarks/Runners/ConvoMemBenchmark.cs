using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// ConvoMem benchmark: conversational memory with sequential turn-by-turn ingest.
/// </summary>
public sealed class ConvoMemBenchmark : BenchmarkBase
{
    public override string Name => "convomem";
    public override string Description => "Conversational memory benchmark (ConvoMem)";

    protected override async Task IngestMemoriesAsync(
        IBackend backend,
        PalaceRef palace,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: true, embedder, ct);

        // Sort by turn order if available
        var sortedItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item.ExpectedAnswer))
            .OrderBy(item => item.Metadata.TryGetValue("turn", out var turn) ? Convert.ToInt32(turn) : int.MaxValue)
            .ThenBy(item => item.Id)
            .ToList();

        if (sortedItems.Count == 0)
            return;

        var texts = sortedItems.Select(item => item.ExpectedAnswer).ToList();
        var embeddings = await embedder.EmbedAsync(texts, ct);
        var records = new List<EmbeddedRecord>();

        for (var i = 0; i < sortedItems.Count; i++)
        {
            var item = sortedItems[i];
            var memId = item.RelevantMemoryIds.FirstOrDefault() ?? item.Id;
            
            records.Add(new EmbeddedRecord(
                Id: memId,
                Document: item.ExpectedAnswer,
                Metadata: item.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                Embedding: embeddings[i]));
        }

        await collection.UpsertAsync(records, ct);
    }
}
