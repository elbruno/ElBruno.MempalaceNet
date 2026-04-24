using System.Diagnostics;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Mining;

/// <summary>
/// Orchestrates mining, embedding, and upserting to a backend.
/// </summary>
public sealed class MiningPipeline
{
    private const int DefaultBatchSize = 32;

    /// <summary>
    /// Runs a mining operation.
    /// </summary>
    public async Task<MiningReport> RunAsync(
        IMiner miner,
        MinerContext ctx,
        IBackend backend,
        IEmbedder embedder,
        string collection,
        CancellationToken ct = default)
    {
        var batchSize = ParseOption(ctx.Options, "batch_size", DefaultBatchSize);
        var stopwatch = Stopwatch.StartNew();
        
        var itemsMined = 0L;
        var batches = 0;
        var embedded = 0L;
        var upserted = 0L;
        var skipped = 0L;
        var errors = new List<string>();
        var seenIds = new HashSet<string>();

        var palace = new PalaceRef(
            Id: Guid.NewGuid().ToString(),
            LocalPath: Environment.CurrentDirectory,
            Namespace: "default");

        ICollection? coll = null;
        
        try
        {
            coll = await backend.GetCollectionAsync(palace, collection, create: true, embedder, ct);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to get collection: {ex.Message}");
            return new MiningReport(0, 0, 0, 0, 0, errors, stopwatch.Elapsed);
        }

        var batch = new List<MinedItem>();
        
        await foreach (var item in miner.MineAsync(ctx, ct))
        {
            itemsMined++;

            // De-dupe within run
            if (seenIds.Contains(item.Id))
            {
                skipped++;
                continue;
            }
            seenIds.Add(item.Id);

            batch.Add(item);

            if (batch.Count >= batchSize)
            {
                var (batchEmbedded, batchUpserted, batchSuccess) = await ProcessBatchAsync(coll, embedder, batch, errors, ct);
                embedded += batchEmbedded;
                upserted += batchUpserted;
                if (batchSuccess) batches++;
                batch.Clear();
            }
        }

        // Process remaining items
        if (batch.Count > 0)
        {
            var (batchEmbedded, batchUpserted, batchSuccess) = await ProcessBatchAsync(coll, embedder, batch, errors, ct);
            embedded += batchEmbedded;
            upserted += batchUpserted;
            if (batchSuccess) batches++;
        }

        stopwatch.Stop();
        
        return new MiningReport(
            ItemsMined: itemsMined,
            Batches: batches,
            Embedded: embedded,
            Upserted: upserted,
            Skipped: skipped,
            Errors: errors,
            Elapsed: stopwatch.Elapsed);
    }

    private static async Task<(long Embedded, long Upserted, bool Success)> ProcessBatchAsync(
        ICollection collection,
        IEmbedder embedder,
        List<MinedItem> batch,
        List<string> errors,
        CancellationToken ct)
    {
        try
        {
            var texts = batch.Select(x => x.Content).ToList();
            var embeddings = await embedder.EmbedAsync(texts, ct);

            var records = new List<EmbeddedRecord>();
            for (var i = 0; i < batch.Count; i++)
            {
                var item = batch[i];
                records.Add(new EmbeddedRecord(
                    Id: item.Id,
                    Document: item.Content,
                    Metadata: item.Metadata,
                    Embedding: embeddings[i]));
            }

            await collection.UpsertAsync(records, ct);
            return (embeddings.Count, records.Count, true);
        }
        catch (Exception ex)
        {
            errors.Add($"Batch processing error: {ex.Message}");
            return (0, 0, false);
        }
    }

    private static int ParseOption(IReadOnlyDictionary<string, string?> options, string key, int defaultValue)
    {
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
            return parsed;
        return defaultValue;
    }
}
