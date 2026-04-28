using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using Microsoft.Extensions.AI;

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
    private readonly IChatClient? _chatClient;

    public WakeUpService(IChatClient? chatClient = null)
    {
        _chatClient = chatClient;
    }

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

        // Generate summary if requested
        string? summary = null;
        if (summarize && _chatClient != null && memories.Count > 0)
        {
            summary = await GenerateSummaryAsync(memories, ct);
        }

        return new WakeUpResult(memories, summary, (int)totalCount);
    }

    private async ValueTask<string> GenerateSummaryAsync(
        IReadOnlyList<EmbeddedRecord> memories,
        CancellationToken ct)
    {
        if (_chatClient == null)
        {
            return "Summary unavailable: No chat client configured.";
        }

        // Build prompt for summarization
        var memoriesText = string.Join("\n", memories.Select((m, i) => 
        {
            var timestamp = m.Metadata.TryGetValue("timestamp", out var ts) 
                ? ParseTimestamp(ts).ToString("yyyy-MM-dd HH:mm") 
                : "unknown";
            var wing = m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : "general";
            return $"{i + 1}. [{timestamp}] [{wing}] {m.Document}";
        }));

        var prompt = $@"You are a helpful assistant summarizing recent memories from a knowledge palace.
Below are {memories.Count} recent memory entries. Please provide a concise, informative summary (2-4 sentences) that captures the key themes, activities, and context. Focus on what would be most useful for someone catching up on recent work.

Memories:
{memoriesText}

Summary:";

        try
        {
            var messages = new[]
            {
                new ChatMessage(ChatRole.System, "You are a helpful assistant that provides concise summaries."),
                new ChatMessage(ChatRole.User, prompt)
            };
            
            var response = await _chatClient.CompleteAsync(messages, cancellationToken: ct);
            return response.Message.Text ?? "Summary generation failed.";
        }
        catch (Exception ex)
        {
            return $"Summary generation failed: {ex.Message}";
        }
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
