# Tyrell — History

## Core Context
- **Project:** MemPalace.NET — port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** Core Engine Dev
- **Reference Python types:** `PalaceRef(id, local_path, namespace)`, `QueryResult(ids, documents, metadatas, distances, embeddings)`, `BaseCollection`, `BaseBackend`. Errors: `BackendError`, `PalaceNotFoundError`, `DimensionMismatchError`, `EmbedderIdentityMismatchError`, `UnsupportedFilterError`.
- **Default backend (Python):** ChromaDB. **Our default (.NET):** SQLite (Microsoft.Data.Sqlite) + sqlite-vec extension or in-table BLOB vectors. Pluggable interface so Qdrant/Chroma adapters can drop in later.

## Learnings

### Phase 1 — Core Domain Implementation (2026-04-24)

**API Decisions:**
- **Records over classes:** All domain types (`PalaceRef`, `EmbeddedRecord`, `Wing`, `Room`, `Drawer`) are immutable records for structural equality and concise syntax
- **ReadOnlyMemory<float> for embeddings:** Zero-copy slicing, better for large vectors than arrays
- **IncludeFields as [Flags] enum:** Composable via bitwise OR, matches Python's kwarg pattern
- **Factory method on HealthStatus:** `Healthy(detail)` / `Unhealthy(detail)` static helpers more ergonomic than constructor
- **WhereClause as discriminated union:** C# records with inheritance model Python's filter DSL elegantly
- **Nested vs flat results:** `QueryResult` uses nested lists (queries × results), `GetResult` uses flat lists — matches Python reference exactly

**Divergences from Python:**
- **Explicit dimensions on ICollection:** Python infers from first insert; .NET makes it a constructor parameter for type safety
- **IComparable.CompareTo():** Python comparison operators work on any object; .NET requires explicit `CompareTo` calls with try/catch for incompatible types
- **Async all the way:** All methods return `ValueTask`; Python reference is sync. Prepares for true async backends (network, disk I/O)
- **No default embedder in GetCollectionAsync:** Python backend can create collections without embedder if dimension is known; .NET requires embedder when create=true (enforces identity tracking)

**In-memory backend lessons:**
- Cosine similarity: `1 - (dot / magnitude)` converts similarity to distance (lower is better) for consistent ordering
- ConcurrentDictionary keyed by palace ID → collection name for thread safety
- Filter evaluation: `IComparable.CompareTo` wrapped in try/catch handles cross-type comparisons gracefully

**Test harness:**
- Abstract `BackendConformanceTests` class lets any backend implementation verify contract compliance
- 18 tests cover: CRUD, upsert idempotence, query ordering, filter operators (Eq, And), count, delete-by-id, delete-by-where, dimension/embedder guards, list/delete collections, backend closed state
- Deterministic FakeEmbedder uses `text.GetHashCode()` as RNG seed for reproducible test vectors

**Next:** Phase 2 will implement `MemPalace.Backends.Sqlite` using this contract, targeting `sqlite-vec` extension for fast vector search.
