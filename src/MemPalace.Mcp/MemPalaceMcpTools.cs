using System.ComponentModel;
using System.Text;
using System.Text.Json;
using MemPalace.Ai.Summarization;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
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
    private readonly IMemorySummarizer _memorySummarizer;
    private readonly IEmbedder _embedder;

    public MemPalaceMcpTools(
        ISearchService searchService,
        IBackend backend,
        IKnowledgeGraph knowledgeGraph,
        IMemorySummarizer memorySummarizer,
        IEmbedder embedder)
    {
        _searchService = searchService;
        _backend = backend;
        _knowledgeGraph = knowledgeGraph;
        _memorySummarizer = memorySummarizer;
        _embedder = embedder;
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

    // ========== WRITE OPERATIONS ==========

    /// <summary>
    /// Store a new memory in the palace.
    /// </summary>
    [McpServerTool]
    [Description("Store a new memory in the palace. Embeds the content and stores it in the specified wing/collection.")]
    public async Task<StoreMemoryResponse> PalaceStoreMemory(
        [Description("The memory content to store")] string content,
        [Description("The collection/wing to store in")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        [Description("Optional metadata (JSON object)")] string? metadata = null,
        CancellationToken ct = default)
    {
        // Parse metadata if provided
        IReadOnlyDictionary<string, object?>? metadataDict = null;
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata);
                metadataDict = parsed;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid metadata JSON: {ex.Message}", ex);
            }
        }
        metadataDict ??= new Dictionary<string, object?>();

        // Get or create collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: true,
            embedder: _embedder,
            ct: ct);

        // Embed content
        var embeddings = await _embedder.EmbedAsync(new[] { content }, ct);
        
        // Generate ID
        var id = Guid.NewGuid().ToString("N");
        var storedAt = DateTimeOffset.UtcNow;
        
        // Add timestamp to metadata
        var enrichedMetadata = new Dictionary<string, object?>(metadataDict)
        {
            ["stored_at"] = storedAt.ToString("O")
        };

        // Store
        var record = new EmbeddedRecord(
            id,
            content,
            enrichedMetadata,
            embeddings[0]);

        await coll.AddAsync(new[] { record }, ct);

        return new StoreMemoryResponse(id, storedAt.ToString("O"));
    }

    /// <summary>
    /// Update an existing memory in the palace.
    /// </summary>
    [McpServerTool]
    [Description("Update an existing memory's content and/or metadata. Re-embeds if content changes.")]
    public async Task<UpdateMemoryResponse> PalaceUpdateMemory(
        [Description("The unique ID of the memory to update")] string id,
        [Description("The collection/wing containing the memory")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        [Description("New content (leave empty to keep existing)")] string? content = null,
        [Description("New metadata (JSON object, leave empty to keep existing)")] string? metadata = null,
        CancellationToken ct = default)
    {
        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Get existing memory
        var existing = await coll.GetAsync(
            ids: new[] { id },
            include: IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Embeddings,
            ct: ct);

        if (existing.Ids.Count == 0)
        {
            throw new InvalidOperationException($"Memory with ID '{id}' not found in collection '{collection}'.");
        }

        var existingDoc = existing.Documents?[0] ?? string.Empty;
        var existingMeta = existing.Metadatas?[0] ?? new Dictionary<string, object?>();
        var existingEmbed = existing.Embeddings?[0] ?? ReadOnlyMemory<float>.Empty;

        // Determine new content
        var newContent = content ?? existingDoc;
        
        // Determine new metadata
        IReadOnlyDictionary<string, object?> newMetadata = existingMeta;
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata);
                newMetadata = parsed ?? existingMeta;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid metadata JSON: {ex.Message}", ex);
            }
        }

        // Re-embed if content changed
        var newEmbedding = existingEmbed;
        if (!string.IsNullOrWhiteSpace(content) && content != existingDoc)
        {
            var embeddings = await _embedder.EmbedAsync(new[] { newContent }, ct);
            newEmbedding = embeddings[0];
        }

        var updatedAt = DateTimeOffset.UtcNow;
        
        // Add updated timestamp to metadata
        var enrichedMetadata = new Dictionary<string, object?>(newMetadata)
        {
            ["updated_at"] = updatedAt.ToString("O")
        };

        // Upsert
        var record = new EmbeddedRecord(
            id,
            newContent,
            enrichedMetadata,
            newEmbedding);

        await coll.UpsertAsync(new[] { record }, ct);

        return new UpdateMemoryResponse(id, updatedAt.ToString("O"));
    }

    /// <summary>
    /// Delete a memory from the palace.
    /// </summary>
    [McpServerTool]
    [Description("Delete a memory from the palace by its unique ID.")]
    public async Task<DeleteMemoryResponse> PalaceDeleteMemory(
        [Description("The unique ID of the memory to delete")] string id,
        [Description("The collection/wing containing the memory")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Delete
        await coll.DeleteAsync(ids: new[] { id }, ct: ct);

        return new DeleteMemoryResponse(true, id);
    }

    // ========== BULK OPERATIONS ==========

    /// <summary>
    /// Export all memories from a wing to JSON format.
    /// </summary>
    [McpServerTool]
    [Description("Export all memories from a wing/collection to JSON format. Returns array of memory objects.")]
    public async Task<ExportWingResponse> PalaceExportWing(
        [Description("The collection/wing to export")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        [Description("Output format: 'json' or 'csv'")] string format = "json",
        CancellationToken ct = default)
    {
        if (format != "json" && format != "csv")
        {
            throw new InvalidOperationException($"Unsupported format '{format}'. Use 'json' or 'csv'.");
        }

        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Get all memories
        var result = await coll.GetAsync(
            ids: null,
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: ct);

        if (format == "json")
        {
            var memories = new List<object>();
            for (int i = 0; i < result.Ids.Count; i++)
            {
                memories.Add(new
                {
                    id = result.Ids[i],
                    document = result.Documents?[i] ?? string.Empty,
                    metadata = result.Metadatas?[i] ?? new Dictionary<string, object?>()
                });
            }

            var json = JsonSerializer.Serialize(memories, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return new ExportWingResponse(collection, result.Ids.Count, format, json);
        }
        else // csv
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,document,metadata");
            
            for (int i = 0; i < result.Ids.Count; i++)
            {
                var id = result.Ids[i];
                var doc = result.Documents?[i] ?? string.Empty;
                var meta = result.Metadatas?[i] ?? new Dictionary<string, object?>();
                var metaJson = JsonSerializer.Serialize(meta);
                
                sb.AppendLine($"\"{id}\",\"{EscapeCsv(doc)}\",\"{EscapeCsv(metaJson)}\"");
            }

            return new ExportWingResponse(collection, result.Ids.Count, format, sb.ToString());
        }
    }

    /// <summary>
    /// Import memories from JSON array.
    /// </summary>
    [McpServerTool]
    [Description("Import memories in bulk from a JSON array. Each item must have 'content' field, optional 'id' and 'metadata'.")]
    public async Task<ImportMemoriesResponse> PalaceImportMemories(
        [Description("JSON array of memories to import")] string jsonContent,
        [Description("The collection/wing to import into")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Parse JSON array
        JsonElement[] items;
        try
        {
            items = JsonSerializer.Deserialize<JsonElement[]>(jsonContent)
                ?? throw new InvalidOperationException("JSON content is null or invalid.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
        }

        // Get or create collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: true,
            embedder: _embedder,
            ct: ct);

        var importedCount = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            try
            {
                // Extract content (required)
                if (!item.TryGetProperty("content", out var contentElem))
                {
                    errors.Add("Missing 'content' field in item");
                    continue;
                }
                var content = contentElem.GetString() ?? string.Empty;

                // Extract ID (optional, generate if missing)
                var id = item.TryGetProperty("id", out var idElem) 
                    ? idElem.GetString() ?? Guid.NewGuid().ToString("N")
                    : Guid.NewGuid().ToString("N");

                // Extract metadata (optional)
                var metadata = new Dictionary<string, object?>();
                if (item.TryGetProperty("metadata", out var metaElem))
                {
                    var metaDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(metaElem.GetRawText());
                    if (metaDict != null)
                    {
                        metadata = metaDict;
                    }
                }

                // Add import timestamp
                metadata["imported_at"] = DateTimeOffset.UtcNow.ToString("O");

                // Embed
                var embeddings = await _embedder.EmbedAsync(new[] { content }, ct);
                
                // Store
                var record = new EmbeddedRecord(id, content, metadata, embeddings[0]);
                await coll.UpsertAsync(new[] { record }, ct);
                
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error importing item: {ex.Message}");
            }
        }

        return new ImportMemoriesResponse(importedCount, errors.ToArray());
    }

    // ========== CONTROL OPERATIONS ==========

    /// <summary>
    /// Wake up and summarize recent memories from a wing.
    /// </summary>
    [McpServerTool]
    [Description("Wake up recent memories from a wing and generate a natural language summary using local LLM. Gracefully falls back to raw list if LLM unavailable.")]
    public async Task<WakeUpResponse> PalaceWakeUp(
        [Description("The collection/wing to wake up from")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        [Description("Number of days to look back (default: 7)")] int days = 7,
        [Description("Maximum number of memories to retrieve (default: 20)")] int limit = 20,
        CancellationToken ct = default)
    {
        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Calculate cutoff date
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        // Get recent memories (filter by stored_at metadata if available)
        var result = await coll.GetAsync(
            ids: null,
            limit: limit,
            include: IncludeFields.Documents | IncludeFields.Metadatas,
            ct: ct);

        // Try to generate summary using LLM
        var summary = await _memorySummarizer.SummarizeAsync(result, ct);

        if (summary != null)
        {
            return new WakeUpResponse(summary, result.Ids.Count, UsedLlm: true);
        }
        else
        {
            // Fallback: return raw list
            var fallbackSummary = new StringBuilder();
            fallbackSummary.AppendLine($"Retrieved {result.Ids.Count} recent memories from '{collection}':");
            fallbackSummary.AppendLine();
            
            for (int i = 0; i < Math.Min(result.Ids.Count, 10); i++)
            {
                var doc = result.Documents?[i] ?? string.Empty;
                var preview = doc.Length > 100 ? doc.Substring(0, 100) + "..." : doc;
                fallbackSummary.AppendLine($"{i + 1}. {preview}");
            }

            if (result.Ids.Count > 10)
            {
                fallbackSummary.AppendLine($"... and {result.Ids.Count - 10} more memories.");
            }

            return new WakeUpResponse(fallbackSummary.ToString(), result.Ids.Count, UsedLlm: false);
        }
    }

    /// <summary>
    /// Get palace statistics.
    /// </summary>
    [McpServerTool]
    [Description("Get statistics about the palace: memory count, wing distribution, embedder identity, backend type.")]
    public async Task<PalaceStatsResponse> PalaceGetStats(
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // List all collections
        var collections = await _backend.ListCollectionsAsync(new PalaceRef(palace), ct);
        
        // Get counts per collection
        var wingStats = new Dictionary<string, long>();
        long totalMemories = 0;

        foreach (var collectionName in collections)
        {
            try
            {
                var coll = await _backend.GetCollectionAsync(
                    new PalaceRef(palace),
                    collectionName,
                    create: false,
                    ct: ct);
                
                var count = await coll.CountAsync(ct);
                wingStats[collectionName] = count;
                totalMemories += count;
            }
            catch
            {
                wingStats[collectionName] = 0;
            }
        }

        return new PalaceStatsResponse(
            palace,
            totalMemories,
            collections.Count,
            wingStats,
            _embedder.ModelIdentity,
            "sqlite" // Hardcoded for now; in future, get from backend metadata
        );
    }

    // ========== EMBEDDER INTROSPECTION ==========
    
    /// <summary>
    /// Get information about the current embedder.
    /// </summary>
    [McpServerTool]
    [Description("Get information about the current embedder (provider, model, dimensions, metadata).")]
    public EmbedderInfoResponse EmbedderInfo()
    {
        var providerName = _embedder is ICustomEmbedder custom 
            ? custom.ProviderName 
            : "unknown";
            
        var metadata = _embedder is ICustomEmbedder customEmbed 
            ? customEmbed.Metadata 
            : new Dictionary<string, object>();
        
        return new EmbedderInfoResponse(
            ModelIdentity: _embedder.ModelIdentity,
            Dimensions: _embedder.Dimensions,
            ProviderName: providerName,
            Metadata: metadata
        );
    }
    
    /// <summary>
    /// List all available embedder providers.
    /// </summary>
    [McpServerTool]
    [Description("List all available embedder providers (Local, OpenAI, AzureOpenAI) with their capabilities.")]
    public EmbedderListResponse EmbedderList()
    {
        return new EmbedderListResponse(
            Embedders: new[]
            {
                new EmbedderDescriptor(
                    ProviderName: "Local",
                    DefaultModel: "sentence-transformers/all-MiniLM-L6-v2",
                    Dimensions: 384,
                    RequiresApiKey: false,
                    Description: "Local ONNX embeddings (ElBruno.LocalEmbeddings). No API key required, runs offline."),
                new EmbedderDescriptor(
                    ProviderName: "OpenAI",
                    DefaultModel: "text-embedding-3-small",
                    Dimensions: 1536,
                    RequiresApiKey: true,
                    Description: "OpenAI embedding API. Requires OPENAI_API_KEY. High quality, low latency."),
                new EmbedderDescriptor(
                    ProviderName: "AzureOpenAI",
                    DefaultModel: "text-embedding-ada-002",
                    Dimensions: 1536,
                    RequiresApiKey: true,
                    Description: "Azure OpenAI embedding API. Requires Azure endpoint, API key, and deployment name.")
            }
        );
    }

    // ========== HELPER METHODS ==========

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"'))
        {
            return value.Replace("\"", "\"\"");
        }
        return value;
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

// Write operations
public record StoreMemoryResponse(string MemoryId, string StoredAt);
public record UpdateMemoryResponse(string MemoryId, string UpdatedAt);
public record DeleteMemoryResponse(bool Deleted, string MemoryId);

// Bulk operations
public record ExportWingResponse(string Wing, int MemoryCount, string Format, string Content);
public record ImportMemoriesResponse(int ImportedCount, string[] Errors);

// Control operations
public record WakeUpResponse(string Summary, int MemoriesProcessed, bool UsedLlm);
public record PalaceStatsResponse(
    string PalaceId,
    long MemoryCount,
    int WingCount,
    IReadOnlyDictionary<string, long> WingStats,
    string Embedder,
    string Backend);

// Embedder introspection
public record EmbedderInfoResponse(
    string ModelIdentity,
    int Dimensions,
    string ProviderName,
    IReadOnlyDictionary<string, object> Metadata);
public record EmbedderListResponse(EmbedderDescriptor[] Embedders);
public record EmbedderDescriptor(
    string ProviderName,
    string DefaultModel,
    int Dimensions,
    bool RequiresApiKey,
    string Description);
