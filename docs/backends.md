# MemPalace.NET — Backend Storage

## Overview

MemPalace.NET uses a pluggable backend architecture for storing embeddings and metadata. The default backend is SQLite-based, offering a zero-dependency local storage solution with brute-force vector similarity search.

## Backend Interface

All backends must implement the `IBackend` and `ICollection` interfaces defined in `MemPalace.Core.Backends`.

### IBackend

The backend is responsible for:
- Creating and managing palace databases
- Creating, listing, and deleting collections within palaces
- Enforcing embedder identity consistency
- Health checking

Key methods:
```csharp
ValueTask<ICollection> GetCollectionAsync(
    PalaceRef palace,
    string collectionName,
    bool create = false,
    IEmbedder? embedder = null,
    CancellationToken ct = default);

ValueTask<IReadOnlyList<string>> ListCollectionsAsync(
    PalaceRef palace, 
    CancellationToken ct = default);

ValueTask DeleteCollectionAsync(
    PalaceRef palace, 
    string name, 
    CancellationToken ct = default);

ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default);
```

### ICollection

The collection is responsible for:
- CRUD operations on embedded records
- Vector similarity search (cosine distance)
- Metadata filtering via `WhereClause` DSL
- Dimension and embedder identity validation

Key methods:
```csharp
ValueTask AddAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default);
ValueTask UpsertAsync(IReadOnlyList<EmbeddedRecord> records, CancellationToken ct = default);
ValueTask<GetResult> GetAsync(IReadOnlyList<string>? ids = null, WhereClause? where = null, ...);
ValueTask<QueryResult> QueryAsync(IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings, int nResults = 10, ...);
ValueTask<long> CountAsync(CancellationToken ct = default);
ValueTask DeleteAsync(IReadOnlyList<string>? ids = null, WhereClause? where = null, ...);
```

### Error Handling

Backends must throw these specific exceptions:
- `PalaceNotFoundException` — palace does not exist when `create=false`
- `EmbedderIdentityMismatchException` — embedder identity doesn't match collection
- `DimensionMismatchException` — embedding dimensions don't match collection
- `UnsupportedFilterException` — backend cannot handle a specific `WhereClause` type
- `BackendClosedException` — backend has been disposed

## Default: SQLite Backend

**Package:** `MemPalace.Backends.Sqlite`  
**Dependencies:** `Microsoft.Data.Sqlite` (9.0.0)

### Architecture

- **One database per palace:** Each `PalaceRef` maps to a `palace.db` file in `{LocalPath}/palace.db` or `{BaseDirectory}/{PalaceId}/palace.db` if no `LocalPath` is provided.
- **One table per collection:** Collections are stored in tables named `collection_{name}`.
- **Metadata table:** `_meta` table stores embedder identity and dimensionality per collection.

### Schema

#### Metadata Table
```sql
CREATE TABLE _meta (
    collection_name TEXT PRIMARY KEY,
    embedder_identity TEXT NOT NULL,
    dimensions INTEGER NOT NULL
)
```

#### Collection Table
```sql
CREATE TABLE [collection_{name}] (
    id TEXT PRIMARY KEY,
    document TEXT NOT NULL,
    metadata TEXT NOT NULL,    -- JSON
    embedding BLOB NOT NULL,    -- float32 array as bytes
    dim INTEGER NOT NULL
)
```

### Vector Storage

**Current implementation:** Embeddings are stored as BLOBs (byte arrays of `float32` values) and searched using **brute-force cosine similarity** in C#.

**Why brute-force?**
- Simple, reliable, no external dependencies
- Sufficient for collections up to ~100K records on modern hardware
- Zero setup overhead

**Future options:**
- **sqlite-vec extension:** Native vector search with HNSW indexing. Currently not available as a stable NuGet package, but could be integrated when available.
- **Microsoft.SemanticKernel.Connectors.Sqlite:** Heavier dependency but Microsoft-stewarded. Considered overkill for MemPalace's needs.

### Cosine Distance Computation

```csharp
distance = 1 - (dot_product / (magnitude_a * magnitude_b))
```

Lower distances indicate higher similarity. Results are sorted ascending by distance.

### Filter Translation

`WhereClause` objects are translated to SQL using SQLite's `json_extract` function:

```csharp
Eq("tag", "test")       → json_extract(metadata, '$.tag') = 'test'
Gt("count", 5)          → json_extract(metadata, '$.count') > 5
In("status", [1, 2])    → json_extract(metadata, '$.status') IN (1, 2)
And([clause1, clause2]) → (clause1) AND (clause2)
```

Supported operators: `Eq`, `NotEq`, `Gt`, `Gte`, `Lt`, `Lte`, `In`, `NotIn`, `And`, `Or`.

Unsupported clauses throw `UnsupportedFilterException`.

### Usage

```csharp
using MemPalace.Backends.Sqlite;

var backend = new SqliteBackend("/path/to/palaces");
var palace = new PalaceRef("my-palace");
var embedder = ...; // your IEmbedder implementation

var collection = await backend.GetCollectionAsync(
    palace, 
    "documents", 
    create: true, 
    embedder: embedder);

// Add records
var records = new[] { 
    new EmbeddedRecord("id1", "hello world", metadata, embedding1),
    ...
};
await collection.AddAsync(records);

// Query
var queryResults = await collection.QueryAsync(
    new[] { queryEmbedding }, 
    nResults: 10);

// Dispose
await backend.DisposeAsync();
```

## Writing a Custom Backend

1. **Create a new project** referencing `MemPalace.Core`.
2. **Implement `IBackend` and `ICollection`**.
3. **Handle all required exceptions** (`PalaceNotFoundException`, etc.).
4. **Pass `BackendConformanceTests`:**
   ```csharp
   public class MyBackendConformanceTests : BackendConformanceTests
   {
       protected override IBackend CreateBackend() => new MyBackend();
   }
   ```
5. **Document vector search strategy** (exact, approximate, hybrid).
6. **Document filter support** (which `WhereClause` types are handled).

### Example: Qdrant Backend Stub

```csharp
public class QdrantBackend : IBackend
{
    private readonly QdrantClient _client;

    public QdrantBackend(string url) 
    { 
        _client = new QdrantClient(url); 
    }

    public async ValueTask<ICollection> GetCollectionAsync(...)
    {
        // Map palace.Id + collectionName to Qdrant collection
        var qdrantCollectionName = $"{palace.Id}_{collectionName}";
        
        if (!await _client.CollectionExistsAsync(qdrantCollectionName))
        {
            if (!create) throw new PalaceNotFoundException(...);
            await _client.CreateCollectionAsync(qdrantCollectionName, ...);
        }

        return new QdrantCollection(_client, qdrantCollectionName, embedder);
    }

    // ... implement other methods
}
```

## Conformance Testing

All backend implementations should pass the `BackendConformanceTests` suite in `MemPalace.Tests`. This ensures:
- Correct CRUD behavior
- Upsert idempotence
- Query ordering (by distance, ascending)
- Filter operators (Eq, And, etc.)
- Dimension and embedder identity guards
- Collection lifecycle (list, delete)
- Backend state management (closed state handling)

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~BackendConformanceTests"
```

## Performance Considerations

**SQLite Backend:**
- **Reads:** O(n) for queries (full scan with filtering)
- **Writes:** O(1) per record (indexed by ID)
- **Space:** ~4 × dimensions × record_count bytes for embeddings

For large collections (>100K records) or latency-critical queries, consider:
- **Qdrant** — distributed, HNSW, GPU-accelerated
- **Chroma** — Python-first, good for hybrid search
- **Pinecone** — managed, serverless, high-scale

All of these can be integrated by implementing the `IBackend` interface.

## Summary

- **Default:** SQLite with brute-force cosine similarity
- **Pluggable:** Implement `IBackend` + `ICollection` for custom backends
- **Tested:** All backends must pass `BackendConformanceTests`
- **Scalable:** Swap to Qdrant/Chroma/Pinecone as needed

For questions or contributions, see the main [README](../README.md).
