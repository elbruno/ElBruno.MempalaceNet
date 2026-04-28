# C# Library Developer Guide

Welcome! This guide is for developers who want to build applications on top of MemPalace.NET or integrate it into your .NET projects.

## Quick Start

MemPalace.NET provides semantic search over local knowledge:

```csharp
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Model;
using MemPalace.Core.Backends;

// Initialize a palace with SQLite storage
var backend = new SqliteBackend("~/my-palaces");
var palace = new PalaceRef("my-palace");
var embedder = new YourEmbedder(); // IEmbedder implementation

// Get or create a collection
var collection = await backend.GetCollectionAsync(
    palace, 
    "documents", 
    create: true, 
    embedder: embedder);

// Add memories
await collection.UpsertAsync(new[]
{
    new EmbeddedRecord(
        Id: "doc-1",
        Document: "Your text content here",
        Metadata: new Dictionary<string, object?> { { "source", "docs" } },
        Embedding: await embedder.EmbedAsync(new[] { "Your text content here" }, default))
});

// Search semantically
var queryEmbedding = await embedder.EmbedAsync(new[] { "search query" }, default);
var results = await collection.QueryAsync(
    new[] { queryEmbedding[0] },
    nResults: 5);

// Display results
for (int i = 0; i < results.Ids[0].Count; i++)
{
    Console.WriteLine($"ID: {results.Ids[0][i]}");
    Console.WriteLine($"Distance: {results.Distances[0][i]:F3}");
    Console.WriteLine($"Document: {results.Documents[0][i]}\n");
}
```

## Core Types Overview

### IEmbedder
**Purpose:** Converts text into vector embeddings for semantic similarity.

```csharp
public interface IEmbedder
{
    string ModelIdentity { get; }        // Unique model identifier
    int Dimensions { get; }              // Embedding vector size (e.g., 384)
    
    // Embed multiple texts at once
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
```

Implement this to swap embedders (ONNX, OpenAI, Ollama, etc.).

### IBackend
**Purpose:** Factory for managing palaces and collections. Handles persistence.

```csharp
public interface IBackend : IAsyncDisposable
{
    // Opens or creates a collection
    ValueTask<ICollection> GetCollectionAsync(
        PalaceRef palace,
        string collectionName,
        bool create = false,
        IEmbedder? embedder = null,
        CancellationToken ct = default);

    // Lists all collections in a palace
    ValueTask<IReadOnlyList<string>> ListCollectionsAsync(
        PalaceRef palace,
        CancellationToken ct = default);

    // Deletes a collection
    ValueTask DeleteCollectionAsync(
        PalaceRef palace,
        string name,
        CancellationToken ct = default);

    // Checks backend health
    ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default);
}
```

Implement this to add custom backends (Postgres, Qdrant, Chroma, etc.).

### ICollection
**Purpose:** Storage and retrieval of embedded records within a palace.

```csharp
public interface ICollection : IAsyncDisposable
{
    string Name { get; }                 // Collection name
    int Dimensions { get; }              // Embedding dimensions
    string EmbedderIdentity { get; }     // Model identity used to create this collection

    // Insert records (fails if ID exists)
    ValueTask AddAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default);

    // Insert or update records
    ValueTask UpsertAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default);

    // Retrieve by ID or metadata filter
    ValueTask<GetResult> GetAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        int? limit = null,
        int offset = 0,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default);

    // Vector similarity search
    ValueTask<QueryResult> QueryAsync(
        IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings,
        int nResults = 10,
        WhereClause? where = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
        CancellationToken ct = default);

    // Record count
    ValueTask<long> CountAsync(CancellationToken ct = default);

    // Delete by ID or filter
    ValueTask DeleteAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        CancellationToken ct = default);
}
```

### EmbeddedRecord
**Purpose:** A single record to store in a collection.

```csharp
public sealed record EmbeddedRecord(
    string Id,                                           // Unique identifier
    string Document,                                     // Text content
    IReadOnlyDictionary<string, object?> Metadata,      // Custom attributes (JSON-like)
    ReadOnlyMemory<float> Embedding);                   // Pre-computed vector (from IEmbedder)
```

### QueryResult
**Purpose:** Results from semantic search (nested lists: queries × results).

```csharp
public sealed record QueryResult(
    IReadOnlyList<IReadOnlyList<string>> Ids,           // Per-query result IDs
    IReadOnlyList<IReadOnlyList<string>> Documents,     // Per-query result documents
    IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Metadatas,  // Per-query metadata
    IReadOnlyList<IReadOnlyList<float>> Distances,      // Cosine distances (lower = more similar)
    IReadOnlyList<IReadOnlyList<ReadOnlyMemory<float>>>? Embeddings = null);  // Optional: embeddings
```

Access results: `results.Documents[queryIndex][resultIndex]`

### GetResult
**Purpose:** Results from direct retrieval (flat lists, not nested).

```csharp
public sealed record GetResult(
    IReadOnlyList<string> Ids,
    IReadOnlyList<string> Documents,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Metadatas,
    IReadOnlyList<ReadOnlyMemory<float>>? Embeddings = null);
```

## Dependency Injection Setup

Use `Microsoft.Extensions.DependencyInjection` to register backends and embedders:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;

var services = new ServiceCollection();

// Register backend
services.AddSingleton<IBackend>(sp => new SqliteBackend("~/my-palaces"));

// Register your embedder
services.AddSingleton<IEmbedder, YourEmbedder>();

// Register palace factory service
services.AddScoped<IPalaceFactory, PalaceFactory>();

var provider = services.BuildServiceProvider();

// Use it
var backend = provider.GetRequiredService<IBackend>();
var embedder = provider.GetRequiredService<IEmbedder>();
```

Example factory service:

```csharp
public interface IPalaceFactory
{
    Task<ICollection> CreateOrGetCollection(string palaceId, string collectionName);
}

public class PalaceFactory : IPalaceFactory
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;

    public PalaceFactory(IBackend backend, IEmbedder embedder)
    {
        _backend = backend;
        _embedder = embedder;
    }

    public async Task<ICollection> CreateOrGetCollection(string palaceId, string collectionName)
    {
        var palace = new PalaceRef(palaceId);
        return await _backend.GetCollectionAsync(
            palace,
            collectionName,
            create: true,
            embedder: _embedder);
    }
}
```

## Common Tasks

### Store a Memory

```csharp
// Prepare text
var text = "Alice joined the engineering team on January 15, 2025.";

// Embed it
var embedding = (await embedder.EmbedAsync(new[] { text }, default))[0];

// Create record
var record = new EmbeddedRecord(
    Id: "memory-1",
    Document: text,
    Metadata: new Dictionary<string, object?>
    {
        { "wing", "team-updates" },
        { "room", "onboarding" },
        { "date", "2025-01-15" },
        { "tags", new[] { "team", "new-hire" } }
    },
    Embedding: embedding);

// Store it
await collection.UpsertAsync(new[] { record });
```

### Search Semantically

```csharp
// User query
var query = "Who recently joined the team?";

// Embed query
var queryEmbedding = (await embedder.EmbedAsync(new[] { query }, default))[0];

// Search
var results = await collection.QueryAsync(
    new[] { queryEmbedding },
    nResults: 5,
    include: IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances);

// Iterate results for the first query
foreach (var i in Enumerable.Range(0, results.Ids[0].Count))
{
    Console.WriteLine($"ID: {results.Ids[0][i]}");
    Console.WriteLine($"Relevance: {(1 - results.Distances[0][i]):F2}");  // Invert distance to similarity
    Console.WriteLine($"Text: {results.Documents[0][i]}\n");
}
```

### Query Knowledge Graph

Use the `MemPalace.KnowledgeGraph` package to store and query entity relationships:

```csharp
using MemPalace.KnowledgeGraph;
using MemPalace.Core.Model;

var kg = new TemporalKnowledgeGraph(backend, palace);

// Add an entity
await kg.AddEntityAsync(
    entityId: "alice",
    entityType: "person",
    properties: new Dictionary<string, object?> { { "name", "Alice Smith" }, { "role", "engineer" } });

// Add a temporal relationship
await kg.AddRelationshipAsync(
    fromId: "alice",
    toId: "project-x",
    relationshipType: "works_on",
    validFrom: new DateTime(2025, 1, 15),
    validTo: null);  // ongoing

// Query as of a specific date
var relationships = await kg.QueryAsync(
    entityId: "alice",
    relationshipType: "works_on",
    asOf: DateTime.UtcNow);

foreach (var rel in relationships)
{
    Console.WriteLine($"{rel.FromId} {rel.Type} {rel.ToId} (valid from {rel.ValidFrom})");
}
```

## Custom Implementations

### When to Write a Custom Embedder

Write a custom `IEmbedder` when:
- You want to use a different model (Ollama, Cohere, proprietary, etc.)
- You need model-specific optimizations or batching
- You want to wrap a third-party embedding library

**Example:** See `examples/CustomEmbedderTemplate/`.

### When to Write a Custom Backend

Write a custom `IBackend` when:
- You want to use a different storage system (Postgres, Qdrant, Chroma, etc.)
- You need cloud-based persistence
- You require advanced indexing or distributed search

**Example:** See `examples/CustomBackendTemplate/`.

## Lifecycle Management

### Palace Lifecycle

```csharp
// Create a palace reference (no I/O)
var palace = new PalaceRef(
    id: "my-palace",
    localPath: "~/my-palaces",  // Optional; determines where data is stored
    namespace: null);            // Optional; for multi-tenant isolation

// Initialize backend (connects to storage)
var backend = new SqliteBackend();

// Get or create a collection
var collection = await backend.GetCollectionAsync(
    palace,
    "documents",
    create: true,
    embedder: embedder);

// Use collection...

// Cleanup
await collection.DisposeAsync();
await backend.DisposeAsync();
```

### Error Handling

```csharp
using MemPalace.Core.Errors;

try
{
    var collection = await backend.GetCollectionAsync(palace, "docs", create: false);
}
catch (PalaceNotFoundException ex)
{
    Console.WriteLine($"Palace not found: {ex.Message}");
}
catch (EmbedderIdentityMismatchException ex)
{
    Console.WriteLine($"Embedder mismatch: {ex.Message}");
    // Collection was created with a different embedder
}
catch (BackendException ex)
{
    Console.WriteLine($"Backend error: {ex.Message}");
}
```

## Best Practices

1. **Reuse backend instances:** Create one `IBackend` per application and share it.
2. **Embed in batches:** Use `EmbedBatchAsync` for multiple texts at once.
3. **Use metadata wisely:** Store searchable attributes (date, tags, source) in metadata.
4. **Test with small datasets first:** Use `InMemoryBackend` for unit tests.
5. **Monitor collection size:** Query performance degrades linearly with collection size.
6. **Use IncludeFields selectively:** Only request what you need (Documents, Metadatas, Embeddings, Distances).
7. **Handle cancellation:** Pass `CancellationToken` to async operations for graceful shutdown.

## Testing

Use `InMemoryBackend` for unit tests:

```csharp
using MemPalace.Core.Backends.InMemory;

[Fact]
public async Task MyTest()
{
    // In-memory backend (no I/O, deterministic)
    using var backend = new InMemoryBackend();
    var palace = new PalaceRef("test-palace");
    var embedder = new TestEmbedder();  // Deterministic embedder

    var collection = await backend.GetCollectionAsync(
        palace,
        "test-collection",
        create: true,
        embedder: embedder);

    // Test your logic...
}
```

## Links

- **Examples:** See `examples/SimpleMemoryAgent/` for a complete working example
- **Custom Backend Template:** `examples/CustomBackendTemplate/`
- **Custom Embedder Template:** `examples/CustomEmbedderTemplate/`
- **Architecture Docs:** `docs/architecture.md`
- **Backend Storage:** `docs/backends.md`
- **API Reference:** Browse `src/MemPalace.Core/` in the GitHub repository
