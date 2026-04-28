# MemPalace.NET — Architecture

## Project Graph

```
MemPalace.Core                  # Domain types + storage interfaces
  ├── Model/                    # PalaceRef, Wing, Room, Drawer, EmbeddedRecord, HealthStatus
  ├── Backends/                 # IBackend, ICollection, IEmbedder, QueryResult, GetResult, WhereClause
  ├── Errors/                   # BackendException hierarchy
  └── Backends/InMemory/        # In-memory backend for testing

MemPalace.Backends.Sqlite       # Default SQLite backend (depends on Core)
MemPalace.Ai                    # Microsoft.Extensions.AI embedder wrappers (depends on Core)
MemPalace.KnowledgeGraph        # Temporal entity graph (depends on Core)
MemPalace.Mcp                   # MCP server (depends on Core, Ai, KnowledgeGraph)
MemPalace.Agents                # Agent Framework integration (depends on Core, Ai)
MemPalace.Cli                   # CLI tool (depends on all above)
MemPalace.Tests                 # xUnit test suite
```

## Backend Contract

### IBackend
- **Purpose:** Factory interface for palace/collection management
- **Key Methods:**
  - `GetCollectionAsync(PalaceRef, name, create?, embedder?)` — Opens or creates a collection
  - `ListCollectionsAsync(PalaceRef)` — Lists all collections in a palace
  - `DeleteCollectionAsync(PalaceRef, name)` — Removes a collection
  - `HealthAsync()` — Backend health check
- **Addressed by:** `PalaceRef(id, localPath?, namespace?)`
- **Errors:** `PalaceNotFoundException`, `EmbedderIdentityMismatchException`, `BackendClosedException`

### ICollection
- **Purpose:** Storage and retrieval of embedded records
- **Properties:** `Name`, `Dimensions`, `EmbedderIdentity`
- **Key Methods:**
  - `AddAsync(records)` — Insert new records (throws on duplicate)
  - `UpsertAsync(records)` — Insert or update
  - `GetAsync(ids?, where?, limit?, offset?, include?)` — Retrieve by ID or filter
  - `QueryAsync(queryEmbeddings, nResults, where?, include?)` — Vector similarity search
  - `CountAsync()` — Record count
  - `DeleteAsync(ids?, where?)` — Delete by ID or filter
- **Results:**
  - `QueryResult` — Nested lists (queries × results)
  - `GetResult` — Flat lists
- **Include Flags:** `Documents`, `Metadatas`, `Distances`, `Embeddings`

### WhereClause
- **Operators:** `Eq`, `NotEq`, `Gt`, `Gte`, `Lt`, `Lte`, `In`, `NotIn`, `And`, `Or`
- **Backend requirement:** Throw `UnsupportedFilterException` if a clause cannot be handled

## Embedder Seam

### IEmbedder
- **Purpose:** Abstraction for embedding models; implementations in `MemPalace.Ai`
- **Properties:** `ModelIdentity` (unique string), `Dimensions`
- **Method:** `EmbedAsync(texts) → ReadOnlyMemory<float>[]`
- **Guard:** Collections store embedder identity; mismatch throws `EmbedderIdentityMismatchException`

## Error Model

```
Exception
└── BackendException
    ├── PalaceNotFoundException
    ├── BackendClosedException
    ├── UnsupportedFilterException
    ├── DimensionMismatchException
    └── EmbedderIdentityMismatchException
```

All backend errors inherit from `BackendException` for uniform handling.

## Testing

- **Conformance harness:** `BackendConformanceTests` (abstract xUnit class)
- **Subclass pattern:** Override `CreateBackend()` to test your backend
- **In-memory backend:** `InMemoryBackend` — brute-force cosine similarity, lives in Core for test reuse
- **Fake embedder:** Deterministic hash-based embedding for reproducible tests

---

## Further Reading

- **[C# Library Developer Guide](guides/csharp-library-developers.md)** — Build applications on top of MemPalace.NET, integrating semantic search into your .NET projects.
