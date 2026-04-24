using System.ComponentModel;
using MemPalace.Core.Backends;
using MemPalace.KnowledgeGraph;
using MemPalace.Search;
using ModelContextProtocol.Server;

namespace MemPalace.Mcp;

/// <summary>
/// MCP server tools exposing the MemPalace API.
/// </summary>
[McpServerToolType]
public class MemPalaceMcpTools
{
    private readonly ISearchService _searchService;
    private readonly IBackend _backend;
    private readonly IKnowledgeGraph _knowledgeGraph;

    public MemPalaceMcpTools(
        ISearchService searchService,
        IBackend backend,
        IKnowledgeGraph knowledgeGraph)
    {
        _searchService = searchService;
        _backend = backend;
        _knowledgeGraph = knowledgeGraph;
    }

    /// <summary>
    /// Search for memories matching the query.
    /// </summary>
    [McpServerTool]
    [Description("Search for memories in the palace matching the query. Returns top_k results with id, document, score, and metadata.")]
    public async Task<SearchResponse> PalaceSearch(
        [Description("The search query text")] string query,
        [Description("The collection/wing to search in")] string collection = "default",
        [Description("Number of results to return")] int topK = 10,
        [Description("Optional wing filter")] string? wing = null,
        [Description("Whether to apply reranking")] bool rerank = false,
        CancellationToken ct = default)
    {
        var opts = new SearchOptions(TopK: topK, Wing: wing, Rerank: rerank);
        var hits = await _searchService.SearchAsync(query, collection, opts, ct);
        
        return new SearchResponse(
            hits.Select(h => new HitResult(
                h.Id,
                h.Document,
                h.Score,
                h.Metadata ?? new Dictionary<string, object?>()
            )).ToArray()
        );
    }

    /// <summary>
    /// Recall memories matching the query (alias for search, LLM-friendly naming).
    /// </summary>
    [McpServerTool]
    [Description("Recall memories from the palace matching the query. This is an alias for palace_search, framed for conversational recall.")]
    public Task<SearchResponse> PalaceRecall(
        [Description("The recall query text")] string query,
        [Description("The collection/wing to recall from")] string collection = "default",
        [Description("Number of memories to recall")] int topK = 10,
        CancellationToken ct = default)
    {
        return PalaceSearch(query, collection, topK, null, false, ct);
    }

    /// <summary>
    /// Get a specific memory by ID.
    /// </summary>
    [McpServerTool]
    [Description("Get a specific memory from the palace by its unique ID.")]
    public async Task<GetResponse> PalaceGet(
        [Description("The unique ID of the memory")] string id,
        [Description("The collection/wing to get from")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Get the collection
        var coll = await _backend.GetCollectionAsync(
            new Core.Model.PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Get by ID
        var result = await coll.GetAsync(
            ids: new[] { id },
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: ct);

        if (result.Ids.Count == 0)
        {
            throw new InvalidOperationException($"Memory with ID '{id}' not found in collection '{collection}'.");
        }

        return new GetResponse(
            result.Ids[0],
            result.Documents?[0] ?? string.Empty,
            result.Metadatas?[0] ?? new Dictionary<string, object?>()
        );
    }

    /// <summary>
    /// List all wings (collections) in the palace.
    /// </summary>
    [McpServerTool]
    [Description("List all wings (collections) available in the palace.")]
    public async Task<ListWingsResponse> PalaceListWings(
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        var collections = await _backend.ListCollectionsAsync(
            new Core.Model.PalaceRef(palace),
            ct);
        
        return new ListWingsResponse(collections.ToArray());
    }

    /// <summary>
    /// Query the knowledge graph for triples matching a pattern.
    /// </summary>
    [McpServerTool]
    [Description("Query the knowledge graph for entity relationships (triples). Use '?' for wildcards in subject, predicate, or object.")]
    public async Task<KgQueryResponse> KgQuery(
        [Description("Subject entity (e.g., 'agent:roy') or '?' for any")] string? subject = null,
        [Description("Predicate/relationship (e.g., 'worked-on') or '?' for any")] string? predicate = null,
        [Description("Object entity (e.g., 'project:MemPalace.Mcp') or '?' for any")] string? @object = null,
        [Description("Query as of this timestamp (ISO8601)")] string? at = null,
        CancellationToken ct = default)
    {
        // Build pattern
        var pattern = new TriplePattern(
            subject == "?" ? null : subject != null ? EntityRef.Parse(subject) : null,
            predicate == "?" ? null : predicate,
            @object == "?" ? null : @object != null ? EntityRef.Parse(@object) : null
        );

        DateTimeOffset? atTime = null;
        if (at != null)
        {
            atTime = DateTimeOffset.Parse(at);
        }

        var triples = await _knowledgeGraph.QueryAsync(pattern, atTime, ct);

        return new KgQueryResponse(
            triples.Select(t => new TripleResult(
                t.Triple.Subject.ToString(),
                t.Triple.Predicate,
                t.Triple.Object.ToString(),
                t.ValidFrom.ToString("O"),
                t.ValidTo?.ToString("O")
            )).ToArray()
        );
    }

    /// <summary>
    /// Get timeline of events for an entity.
    /// </summary>
    [McpServerTool]
    [Description("Get a timeline of events (relationships) for an entity over time. Optionally filter by date range.")]
    public async Task<KgTimelineResponse> KgTimeline(
        [Description("Entity reference (e.g., 'agent:roy', 'project:MemPalace.Core')")] string entity,
        [Description("Start date (ISO8601, optional)")] string? from = null,
        [Description("End date (ISO8601, optional)")] string? to = null,
        CancellationToken ct = default)
    {
        var entityRef = EntityRef.Parse(entity);
        
        DateTimeOffset? fromTime = null;
        DateTimeOffset? toTime = null;
        if (from != null)
        {
            fromTime = DateTimeOffset.Parse(from);
        }
        if (to != null)
        {
            toTime = DateTimeOffset.Parse(to);
        }

        var timeline = await _knowledgeGraph.TimelineAsync(entityRef, fromTime, toTime, ct);

        return new KgTimelineResponse(
            timeline.Select(e => new TimelineEventResult(
                e.At.ToString("O"),
                e.Entity.ToString(),
                e.Predicate,
                e.Other.ToString(),
                e.Direction
            )).ToArray()
        );
    }

    /// <summary>
    /// Check the health status of the palace backend.
    /// </summary>
    [McpServerTool]
    [Description("Check the health and status of the MemPalace backend.")]
    public async Task<HealthResponse> PalaceHealth(CancellationToken ct = default)
    {
        var health = await _backend.HealthAsync(ct);
        
        return new HealthResponse(
            health.Ok,
            health.Detail
        );
    }
}

// Response DTOs for MCP tools
public record SearchResponse(HitResult[] Hits);
public record HitResult(string Id, string Document, float Score, IReadOnlyDictionary<string, object?> Metadata);

public record GetResponse(string Id, string Document, IReadOnlyDictionary<string, object?> Metadata);

public record ListWingsResponse(string[] Wings);

public record KgQueryResponse(TripleResult[] Triples);
public record TripleResult(string Subject, string Predicate, string Object, string ValidFrom, string? ValidTo);

public record KgTimelineResponse(TimelineEventResult[] Events);
public record TimelineEventResult(string Timestamp, string Entity, string Predicate, string Other, string Direction);

public record HealthResponse(bool Ok, string Detail);
