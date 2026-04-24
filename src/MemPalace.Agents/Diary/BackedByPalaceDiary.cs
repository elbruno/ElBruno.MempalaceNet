using System.Text.Json;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Search;

namespace MemPalace.Agents.Diary;

public class BackedByPalaceDiary : IAgentDiary
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly ISearchService _searchService;
    private readonly PalaceRef _palaceRef = new("agents");

    public BackedByPalaceDiary(
        IBackend backend,
        IEmbedder embedder,
        ISearchService searchService)
    {
        _backend = backend;
        _embedder = embedder;
        _searchService = searchService;
    }

    public async Task AppendAsync(
        string agentId,
        DiaryEntry entry,
        CancellationToken ct = default)
    {
        var collectionName = $"agent_diary:{agentId}";
        var collection = await _backend.GetCollectionAsync(
            _palaceRef,
            collectionName,
            create: true,
            ct: ct);

        var metadata = new Dictionary<string, object?>
        {
            ["agent_id"] = entry.AgentId,
            ["at"] = entry.At.ToString("O"),
            ["role"] = entry.Role
        };
        
        if (entry.Metadata != null)
        {
            foreach (var kvp in entry.Metadata)
            {
                metadata[$"meta_{kvp.Key}"] = kvp.Value;
            }
        }

        var embeddings = await _embedder.EmbedAsync(new[] { entry.Content }, ct);

        var record = new EmbeddedRecord(
            Guid.NewGuid().ToString(),
            entry.Content,
            metadata,
            embeddings[0]);

        await collection.AddAsync(new[] { record }, ct);
    }

    public async Task<IReadOnlyList<DiaryEntry>> RecentAsync(
        string agentId,
        int take = 50,
        CancellationToken ct = default)
    {
        var collectionName = $"agent_diary:{agentId}";
        
        try
        {
            var collection = await _backend.GetCollectionAsync(
                _palaceRef,
                collectionName,
                create: false,
                ct: ct);

            var result = await collection.GetAsync(
                ids: null,
                include: IncludeFields.Documents | IncludeFields.Metadatas,
                limit: take,
                ct: ct);

            return result.Ids
                .Select((id, idx) => ParseDiaryEntry(
                    id,
                    result.Documents?[idx] ?? string.Empty,
                    result.Metadatas?[idx] ?? new Dictionary<string, object?>()))
                .OrderByDescending(e => e.At)
                .Take(take)
                .ToList();
        }
        catch
        {
            return Array.Empty<DiaryEntry>();
        }
    }

    public async Task<IReadOnlyList<DiaryEntry>> SearchAsync(
        string agentId,
        string query,
        int topK = 10,
        CancellationToken ct = default)
    {
        var collectionName = $"agent_diary:{agentId}";

        try
        {
            var opts = new SearchOptions(TopK: topK, Wing: null, Rerank: false);
            var hits = await _searchService.SearchAsync(
                query,
                collectionName,
                opts,
                ct);

            return hits
                .Select(h => ParseDiaryEntry(h.Id, h.Document, h.Metadata ?? new Dictionary<string, object?>()))
                .ToList();
        }
        catch
        {
            return Array.Empty<DiaryEntry>();
        }
    }

    private static DiaryEntry ParseDiaryEntry(
        string id,
        string content,
        IReadOnlyDictionary<string, object?> metadata)
    {
        var agentId = metadata.GetValueOrDefault("agent_id")?.ToString() ?? "unknown";
        var atStr = metadata.GetValueOrDefault("at")?.ToString();
        var at = DateTimeOffset.TryParse(atStr, out var parsed) ? parsed : DateTimeOffset.UtcNow;
        var role = metadata.GetValueOrDefault("role")?.ToString() ?? "unknown";

        var customMetadata = metadata
            .Where(kvp => kvp.Key.StartsWith("meta_"))
            .ToDictionary(kvp => kvp.Key.Substring(5), kvp => kvp.Value);

        return new DiaryEntry(agentId, at, role, content, customMetadata);
    }
}
