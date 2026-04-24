using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// LoCoMo benchmark: long-context conversational Q&A.
/// </summary>
public sealed class LoCoMoBenchmark : BenchmarkBase
{
    public override string Name => "locomo";
    public override string Description => "Long-context conversational Q&A (LoCoMo)";

    protected override async Task IngestMemoriesAsync(
        IBackend backend,
        IEmbedder embedder,
        IReadOnlyList<DatasetItem> items,
        CancellationToken ct)
    {
        var palace = new PalaceRef(
            Id: Guid.NewGuid().ToString(),
            LocalPath: Environment.CurrentDirectory,
            Namespace: "benchmark");

        var collection = await backend.GetCollectionAsync(palace, DefaultCollection, create: true, embedder, ct);

        // Ingest conversation episodes as memories
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
