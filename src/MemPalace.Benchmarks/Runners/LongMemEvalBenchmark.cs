using MemPalace.Benchmarks.Core;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Benchmarks.Runners;

/// <summary>
/// LongMemEval benchmark: long-conversation memory recall.
/// Dataset expects conversational turns with memory IDs.
/// </summary>
public sealed class LongMemEvalBenchmark : BenchmarkBase
{
    public override string Name => "longmemeval";
    public override string Description => "Long-conversation memory recall (LongMemEval)";

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
}
