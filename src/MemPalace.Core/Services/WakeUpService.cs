using MemPalace.Core.Backends;
using MemPalace.Core.Model;

namespace MemPalace.Core.Services;

/// <summary>
/// Service for retrieving and optionally summarizing recent memories (wake-up).
/// </summary>
public interface IWakeUpService
{
    /// <summary>
    /// Retrieves recent memories from a collection, optionally with summarization.
    /// </summary>
    /// <param name="collection">The collection to retrieve from.</param>
    /// <param name="limit">Maximum number of memories to retrieve.</param>
    /// <param name="where">Optional filter clause.</param>
    /// <param name="summarize">Whether to generate a summary of the memories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Wake-up result containing memories and optional summary.</returns>
    ValueTask<WakeUpResult> WakeUpAsync(
        ICollection collection,
        int limit = 20,
        WhereClause? where = null,
        bool summarize = false,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a wake-up operation.
/// </summary>
public sealed record WakeUpResult(
    IReadOnlyList<EmbeddedRecord> Memories,
    string? Summary = null,
    int TotalCount = 0);

/// <summary>
/// Default implementation of wake-up service.
/// </summary>
public sealed class WakeUpService : IWakeUpService
{
    public async ValueTask<WakeUpResult> WakeUpAsync(
        ICollection collection,
        int limit = 20,
        WhereClause? where = null,
        bool summarize = false,
        CancellationToken ct = default)
    {
        // Retrieve recent memories (limit + order by timestamp would be ideal, but not supported yet)
        var result = await collection.GetAsync(
            ids: null,
            where: where,
            limit: limit,
            offset: 0,
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: ct);

        // Sort by timestamp in metadata (if available), most recent first
        var sorted = result.Documents
            .Select((doc, idx) => new
            {
                Id = result.Ids[idx],
                Document = doc,
                Metadata = result.Metadatas?[idx] ?? new Dictionary<string, object?>(),
                Timestamp = result.Metadatas?[idx]?.TryGetValue("timestamp", out var ts) == true
                    ? ParseTimestamp(ts)
                    : DateTime.MinValue
            })
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToList();

        // Reconstruct EmbeddedRecords (without embeddings for efficiency)
        var memories = sorted
            .Select(x => new EmbeddedRecord(
                x.Id,
                x.Document,
                x.Metadata,
                ReadOnlyMemory<float>.Empty))
            .ToList();

        // Get total count
        var totalCount = await collection.CountAsync(ct);

        // TODO: Implement summarization using IChatClient when available
        // For now, summarization is handled at the CLI level or can be added as an extension method
        string? summary = null;

        return new WakeUpResult(memories, summary, (int)totalCount);
    }

    private static DateTime ParseTimestamp(object? value)
    {
        if (value == null) return DateTime.MinValue;
        
        if (value is DateTime dt) return dt;
        if (value is DateTimeOffset dto) return dto.UtcDateTime;
        if (value is string str && DateTime.TryParse(str, out var parsed)) return parsed;
        if (value is long ticks) return new DateTime(ticks, DateTimeKind.Utc);
        
        return DateTime.MinValue;
    }
}
