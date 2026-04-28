using System.ComponentModel;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Mcp.Security;
using ModelContextProtocol.Server;

namespace MemPalace.Mcp.Tools;

/// <summary>
/// MCP write tools for storing and modifying memories.
/// </summary>
[McpServerToolType]
public class WriteTools
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly SecurityValidator _validator;
    private readonly IConfirmationPrompt _confirmationPrompt;

    public WriteTools(
        IBackend backend,
        IEmbedder embedder,
        SecurityValidator validator,
        IConfirmationPrompt confirmationPrompt)
    {
        _backend = backend;
        _embedder = embedder;
        _validator = validator;
        _confirmationPrompt = confirmationPrompt;
    }

    /// <summary>
    /// Store a new memory in the palace.
    /// </summary>
    [McpServerTool]
    [Description("Store a new memory in the palace. Returns the ID of the stored memory.")]
    public async Task<StoreResponse> PalaceStore(
        [Description("The content/document to store")] string content,
        [Description("The collection/wing to store in")] string collection = "default",
        [Description("Optional metadata as JSON object")] Dictionary<string, object>? metadata = null,
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateCollectionName(collection);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be empty", nameof(content));
        }

        // Generate embedding
        var embeddings = await _embedder.EmbedAsync(new[] { content }, ct);
        var embedding = embeddings[0];
        
        // Generate ID
        var id = Guid.NewGuid().ToString("N");

        // Get or create collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: true,
            ct: ct);

        // Store record
        var metadataDict = metadata != null 
            ? new Dictionary<string, object?>(metadata.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)))
            : new Dictionary<string, object?>();

        var record = new EmbeddedRecord(
            Id: id,
            Embedding: embedding,
            Document: content,
            Metadata: metadataDict
        );

        await coll.AddAsync(new[] { record }, ct);

        // Audit log
        await _validator.AuditWriteOperationAsync("palace_store", collection, id, metadata, ct);

        return new StoreResponse(id, "stored");
    }

    /// <summary>
    /// Update an existing memory.
    /// </summary>
    [McpServerTool]
    [Description("Update an existing memory in the palace. Can update content and/or metadata.")]
    public async Task<UpdateResponse> PalaceUpdate(
        [Description("The ID of the memory to update")] string id,
        [Description("New content (optional)")] string? content = null,
        [Description("New metadata (optional)")] Dictionary<string, object>? metadata = null,
        [Description("The collection/wing")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateMemoryId(id);
        _validator.ValidateCollectionName(collection);

        if (content == null && metadata == null)
        {
            throw new ArgumentException("Must provide either content or metadata to update");
        }

        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Get existing record
        var existing = await coll.GetAsync(ids: new[] { id }, ct: ct);
        if (existing.Ids.Count == 0)
        {
            throw new InvalidOperationException($"Memory with ID '{id}' not found");
        }

        // Prepare updated record
        var updatedContent = content ?? existing.Documents![0];
        var updatedMetadata = metadata != null 
            ? new Dictionary<string, object?>(metadata.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)))
            : existing.Metadatas![0];

        // Generate new embedding if content changed
        ReadOnlyMemory<float> embedding;
        if (content != null)
        {
            var embeddings = await _embedder.EmbedAsync(new[] { content }, ct);
            embedding = embeddings[0];
        }
        else
        {
            // Retrieve existing embedding
            var queryResult = await coll.QueryAsync(
                new[] { ReadOnlyMemory<float>.Empty },
                nResults: 1,
                ct: ct);
            embedding = queryResult.Embeddings![0][0];
        }

        var record = new EmbeddedRecord(
            Id: id,
            Embedding: embedding,
            Document: updatedContent,
            Metadata: updatedMetadata
        );

        await coll.UpsertAsync(new[] { record }, ct);

        // Audit log
        await _validator.AuditWriteOperationAsync("palace_update", collection, id, metadata, ct);

        return new UpdateResponse(id, "updated");
    }

    /// <summary>
    /// Delete a memory from the palace.
    /// </summary>
    [McpServerTool]
    [Description("Delete a memory from the palace. This is a destructive operation that requires confirmation.")]
    public async Task<DeleteResponse> PalaceDelete(
        [Description("The ID of the memory to delete")] string id,
        [Description("The collection/wing")] string collection = "default",
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateMemoryId(id);
        _validator.ValidateCollectionName(collection);

        // Confirmation prompt
        var confirmed = await _confirmationPrompt.ConfirmAsync(
            "delete memory",
            $"{collection}/{id}",
            ct);

        if (!confirmed)
        {
            return new DeleteResponse(id, "cancelled");
        }

        // Get collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: false,
            ct: ct);

        // Delete
        await coll.DeleteAsync(ids: new[] { id }, ct: ct);

        // Audit log
        await _validator.AuditWriteOperationAsync("palace_delete", collection, id, null, ct);

        return new DeleteResponse(id, "deleted");
    }

    /// <summary>
    /// Store multiple memories in batch.
    /// </summary>
    [McpServerTool]
    [Description("Store multiple memories in batch (max 100). Returns the IDs of stored memories.")]
    public async Task<BatchStoreResponse> PalaceBatchStore(
        [Description("Array of documents to store")] string[] documents,
        [Description("The collection/wing to store in")] string collection = "default",
        [Description("Optional array of metadata (must match documents length)")] Dictionary<string, object>[]? metadataArray = null,
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateCollectionName(collection);
        _validator.ValidateBatchSize(documents.Length);

        if (metadataArray != null && metadataArray.Length != documents.Length)
        {
            throw new ArgumentException("Metadata array length must match documents length");
        }

        // Generate embeddings
        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var doc in documents)
        {
            var embResult = await _embedder.EmbedAsync(new[] { doc }, ct);
            embeddings.Add(embResult[0]);
        }

        // Get or create collection
        var coll = await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: true,
            ct: ct);

        // Create records
        var records = new List<EmbeddedRecord>();
        var ids = new List<string>();
        
        for (int i = 0; i < documents.Length; i++)
        {
            var id = Guid.NewGuid().ToString("N");
            ids.Add(id);
            
            var metadataDict = metadataArray?[i] != null 
                ? new Dictionary<string, object?>(metadataArray[i].Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)))
                : new Dictionary<string, object?>();

            records.Add(new EmbeddedRecord(
                Id: id,
                Embedding: embeddings[i],
                Document: documents[i],
                Metadata: metadataDict
            ));
        }

        await coll.AddAsync(records, ct);

        // Audit log
        await _validator.AuditWriteOperationAsync(
            "palace_batch_store", 
            collection, 
            null, 
            new Dictionary<string, object> { ["count"] = documents.Length },
            ct);

        return new BatchStoreResponse(ids.ToArray(), "stored", documents.Length);
    }

    /// <summary>
    /// Create a new collection.
    /// </summary>
    [McpServerTool]
    [Description("Create a new collection/wing in the palace.")]
    public async Task<CreateCollectionResponse> PalaceCreateCollection(
        [Description("The name of the collection to create")] string collection,
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateCollectionName(collection);

        // Create collection
        await _backend.GetCollectionAsync(
            new PalaceRef(palace),
            collection,
            create: true,
            ct: ct);

        // Audit log
        await _validator.AuditWriteOperationAsync("palace_create_collection", collection, null, null, ct);

        return new CreateCollectionResponse(collection, "created");
    }

    /// <summary>
    /// Delete an entire collection.
    /// </summary>
    [McpServerTool]
    [Description("Delete an entire collection/wing from the palace. This is a destructive operation that requires confirmation.")]
    public async Task<DeleteCollectionResponse> PalaceDeleteCollection(
        [Description("The name of the collection to delete")] string collection,
        [Description("The palace reference (default: 'default')")] string palace = "default",
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateCollectionName(collection);

        // Confirmation prompt
        var confirmed = await _confirmationPrompt.ConfirmAsync(
            "delete collection",
            collection,
            ct);

        if (!confirmed)
        {
            return new DeleteCollectionResponse(collection, "cancelled");
        }

        // Delete collection
        await _backend.DeleteCollectionAsync(
            new PalaceRef(palace),
            collection,
            ct: ct);

        // Audit log
        await _validator.AuditWriteOperationAsync("palace_delete_collection", collection, null, null, ct);

        return new DeleteCollectionResponse(collection, "deleted");
    }
}

// Response DTOs
public record StoreResponse(string Id, string Status);
public record UpdateResponse(string Id, string Status);
public record DeleteResponse(string Id, string Status);
public record BatchStoreResponse(string[] Ids, string Status, int Count);
public record CreateCollectionResponse(string Collection, string Status);
public record DeleteCollectionResponse(string Collection, string Status);
