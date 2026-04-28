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
        // Use the optimized backend method
        var memories = await collection.WakeUpAsync(
            limit: limit,
            where: where,
            sinceDate: null,
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: ct);

        // Get total count
        var totalCount = await collection.CountAsync(ct);

        // Generate summary if requested and chat client is available
        string? summary = null;
        if (summarize && memories.Count > 0)
        {
            summary = await GenerateSummaryAsync(memories, ct);
        }

        return new WakeUpResult(memories, summary, (int)totalCount);
    }

    private async Task<string> GenerateSummaryAsync(
        IReadOnlyList<EmbeddedRecord> memories,
        CancellationToken ct)
    {
        if (_chatClient == null)
        {
            // Simple text summary fallback
            return GenerateTextSummary(memories);
        }

        // Build context from memories
        var context = string.Join("\n\n", memories.Select((m, i) =>
        {
            var timestamp = m.Metadata.TryGetValue("timestamp", out var ts)
                ? ParseTimestamp(ts)
                : DateTime.MinValue;
            var timeStr = timestamp != DateTime.MinValue
                ? timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                : "unknown time";
            var wing = m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : "";
            var room = m.Metadata.TryGetValue("room", out var r) ? r?.ToString() : "";
            var location = string.IsNullOrEmpty(wing) ? "" : $" [{wing}{(string.IsNullOrEmpty(room) ? "" : $"/{room}")}]";
            return $"{i + 1}. ({timeStr}{location}): {m.Document}";
        }));

        // TODO: Implement LLM summarization using IChatClient when available
        // For now, use the text fallback
        return GenerateTextSummary(memories);
    }

    private static string GenerateTextSummary(IReadOnlyList<EmbeddedRecord> memories)
    {
        // Generate a simple text summary without LLM
        var wingGroups = memories
            .GroupBy(m => m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : "general")
            .ToList();

        var summary = $"Recent activity summary ({memories.Count} memories):\n\n";
        
        foreach (var group in wingGroups)
        {
            summary += $"**{group.Key}** ({group.Count()} memories)\n";
            var recent = group.Take(3);
            foreach (var memory in recent)
            {
                var preview = memory.Document.Length > 80 
                    ? memory.Document.Substring(0, 77) + "..." 
                    : memory.Document;
                summary += $"  - {preview}\n";
            }
            summary += "\n";
        }

        return summary.TrimEnd();
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
