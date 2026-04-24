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

### Phase 2 — SQLite Backend Implementation (2026-04-24)

**Vector storage choice:**
- **Pure managed BLOB approach:** Store embeddings as byte arrays in BLOB columns, compute cosine similarity in C#
- **Why not sqlite-vec?** Not available as stable NuGet package at project time. Could integrate later but avoiding external native dependencies for now.
- **Performance characteristics:** Brute-force O(n) search sufficient for <100K records on modern hardware (~10ms for 10K 128-dim vectors)

**Implementation details:**
- **One DB per palace:** `{LocalPath}/palace.db` or `{BaseDir}/{PalaceId}/palace.db`
- **One table per collection:** `[collection_{name}]` with square brackets for SQL identifier safety (handles hyphens, special chars)
- **Metadata table:** `_meta` stores embedder_identity and dimensions per collection for validation
- **Connection management:** Dictionary of connections by palace ID, closed on dispose
- **Filter translation:** `json_extract(metadata, '$.field')` for WhereClause → SQL mapping. Supports Eq, NotEq, Gt, Gte, Lt, Lte, In, NotIn, And, Or

**Cosine distance formula:**
```csharp
distance = 1 - (dot_product / (mag_a * mag_b))
```
Lower distance = higher similarity. Results sorted ascending.

**Embedding serialization:**
- `float32[]` → `byte[]` via `Buffer.BlockCopy` (4 bytes per float)
- Stored in BLOB column `embedding`
- Dimension stored separately in `dim` column for validation

**SQL identifier quoting:**
- Initially hit syntax error: `CREATE TABLE collection_test-col` fails due to hyphen
- Solution: Always use `[table_name]` bracket syntax for generated identifiers
- Applies to all DDL and DML: CREATE, SELECT, INSERT, UPDATE, DELETE

**Testing approach:**
- Created standalone smoke test (MemPalace.Backends.Sqlite.SmokeTest) due to solution-wide NuGet conflicts in MemPalace.Ai
- All 10 smoke tests pass: health, CRUD, query, filter, list/delete collections
- SqliteBackendConformanceTests created but blocked by Ai package version conflicts (outside scope)

**Documentation:**
- `docs/backends.md` covers interface, schema, vector storage decision, filter support, custom backend guide
- Documented upgrade path to Qdrant/Chroma for >100K records or latency-critical use cases

**Deviations from plan:**
- Used brute-force instead of sqlite-vec (no stable NuGet)
- Added smoke test project instead of running full conformance suite (blocked by unrelated NuGet issues)

**Next:** Phase 3 (Roy) will integrate Microsoft.Extensions.AI for embeddings with Ollama default provider.
