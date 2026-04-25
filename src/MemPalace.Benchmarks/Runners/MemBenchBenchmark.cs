using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// MemBench benchmark: general memory recall on diverse statements.
/// </summary>
public sealed class MemBenchBenchmark : BenchmarkBase
{
    public override string Name => "membench";
    public override string Description => "General memory benchmark (MemBench)";

    protected override async Task IngestMemoriesAsync(
        IBackend backend,
        PalaceRef palace,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: true, embedder, ct);

        // Ingest all statements as memories
        var memories = items
            .Where(item => !string.IsNullOrWhiteSpace(item.ExpectedAnswer))
            .ToList();

        if (memories.Count == 0)
            return;

        var texts = memories.Select(item => item.ExpectedAnswer).ToList();
        var embeddings = await embedder.EmbedAsync(texts, ct);
        var records = new List<EmbeddedRecord>();

        for (var i = 0; i < memories.Count; i++)
        {
            var item = memories[i];
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
