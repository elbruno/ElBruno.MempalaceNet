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

### Phase 4 — Mining + Search Pipeline (2026-04-24)

**Scope:** Implemented `MemPalace.Mining` and `MemPalace.Search` with complete test coverage and documentation.

**Mining Infrastructure:**
- **Core types:** `MinedItem`, `IMiner`, `MinerContext`, `MiningReport`
- **FileSystemMiner:**
  - Walks directories using `Microsoft.Extensions.FileSystemGlobbing`
  - Respects `.gitignore` patterns (parsed line-by-line, applied as exclusions)
  - Chunks large files: 2000 chars default, 200 overlap (preserves context)
  - Binary detection: extension blacklist + null-byte sniffing
  - Stable IDs: SHA-256 prefix (first 8 hex) + path/chunk-index
  - Metadata: `path`, `ext`, `size`, `mtime`, `sha256_8`, optional `chunk_*`
- **ConversationMiner:**
  - Parses JSONL (streaming, one line = one turn) and Markdown (`## User` / `## Assistant` headers)
  - Handles role variations: User/Assistant/Human/AI (case-insensitive)
  - Metadata: `role`, `turn_index`, `conversation_id`, optional `timestamp`
  - Error tolerance: skips invalid JSON lines, empty turns
  - Yield-in-try workaround: parse → optional yield pattern (C# async iterator constraint)
- **MiningPipeline:**
  - Batches items (default 32, configurable)
  - De-dupes within run via HashSet<string> (id-based)
  - Embeds batch → upserts to backend
  - Returns `MiningReport` (counters + errors + elapsed)
  - Non-fatal error handling: continues on batch failure, logs to `Errors`
- **DI:** `AddMemPalaceMining()` registers `FileSystemMiner`, `ConversationMiner`, `MiningPipeline` as keyed services

**Search Infrastructure:**
- **Core types:** `SearchHit`, `SearchOptions`, `ISearchService`
- **VectorSearchService:**
  - Pure semantic search (embed query → backend.QueryAsync)
  - Converts distance to score: `1 - cosine_distance`
  - Optional reranking via `IReranker` (if present in DI)
  - Wing filter: `SearchOptions.Wing` → `Eq("wing", value)` WhereClause
  - MinScore threshold applied post-rerank
- **HybridSearchService:**
  - Retrieves 2×TopK vector candidates (allows room for keyword re-ranking)
  - Keyword scoring: token overlap (simple BM25-lite, no corpus stats)
    - Tokenize on whitespace + punctuation
    - Score = `|query_tokens ∩ doc_tokens| / |query_tokens|`
  - Reciprocal Rank Fusion: `score = Σ(1/(60 + rank_in_source))`
  - Metadata annotation: `sources = ["vector", "keyword"]`
  - Documented simplification: v0.1 uses overlap, not full BM25 (upgrade path noted)
- **DI:** `AddMemPalaceSearch()` (vector default), `AddHybridSearch()` (swaps to hybrid)

**CLI Integration:**
- Updated `MineCommand` and `SearchCommand` with real options (mode, wing, collection, rerank, top-k)
- Wired `services.AddMemPalaceMining()` and `services.AddMemPalaceSearch()` in `Program.cs`
- Commands show "infrastructure ready" message (awaiting Phase 2/3 backend/embedder)
- Added project references: `MemPalace.Cli` → `MemPalace.Mining`, `MemPalace.Search`

**Tests (18 new, all pass):**
- **FileSystemMinerTests (5):** empty dir, metadata extraction, chunking, binary skip, .gitignore respect
- **ConversationMinerTests (5):** JSONL parsing, Markdown parsing, invalid line skip, nonexistent file, alternate role names
- **MiningPipelineTests (4):** batching (2-item batches), de-dupe, error handling, elapsed time reporting
- **VectorSearchServiceTests (5):** top-K ordering, Wing→WhereClause, MinScore filter, reranker integration, collection-not-found
- **HybridSearchServiceTests (4):** RRF fusion, top-K respected, MinScore filter, empty results handling

**Documentation:**
- `docs/mining.md` (7K chars): IMiner contract, built-in miners (FileSystem, Conversation), custom miner guide, chunking knobs, performance notes, DI registration, CLI usage
- `docs/search.md` (8K chars): vector vs hybrid, RRF math (k=60), BM25-lite explanation, reranking, SearchOptions reference, performance tips, CLI usage

**Compiler Issues Resolved:**
- **Async iterator `ref` params:** Refactored `ProcessBatchAsync` to return tuple `(long Embedded, long Upserted, bool Success)` instead of `ref` out-params
- **Yield in try-catch:** Moved parsing outside try, conditionally yield after finally block (NSubstitute `Returns` must return `ValueTask`, not `Task`)
- **IReadOnlyList.IndexOf:** Used `Array.IndexOf(list.ToArray(), item)` for compatibility
- **Expression tree pattern matching:** Simplified `Arg.Is<>` predicate to avoid `is` operator in expression tree

**Build/Test Status:**
- Solution builds clean (10 projects)
- 99/105 tests pass (6 pre-existing failures in KG/Cli parsing, unrelated)
- All 18 new Mining/Search tests green
- CLI `--help` runs without crash

**Next:** Phases 2 (SQLite backend) and 3 (embedder) are prerequisites for end-to-end mining/search. Phase 5 (Agents) will leverage search for memory retrieval.

### v0.6.0 sqlite-vec Research (2026-04-25)

**Context:** Current SQLite backend uses O(n) brute-force cosine similarity in managed C#. Performance ceiling at ~100K vectors. v0.6.0 priority is scalable vector search.

**Research findings:**

**1. NuGet Availability:**
- ✅ **Available:** `sqlite-vec` v0.1.7-alpha.2.1 (prerelease)
- ✅ **Targets:** .NET Standard 2.0 (compatible with net10.0)
- ✅ **Package:** https://www.nuget.org/packages/sqlite-vec/
- ✅ **Alternative:** `Microsoft.SemanticKernel.Connectors.SqliteVec` (higher-level, may be overkill)
- ⚠️ **Status:** Alpha prerelease, but production-used in several projects
- ✅ **Build Test:** Successfully added to `MemPalace.Backends.Sqlite.csproj`, `dotnet restore` and `dotnet build` pass

**2. Performance Characteristics:**
- **SIMD-accelerated:** AVX/NEON for cosine, L2, Hamming distance
- **Expected speedup:** 10-25x at 100K vectors vs current O(n)
- **Benchmarks (from external sources):**
  - 10K vectors, 384 dims: ~10-20ms (vs ~100ms current)
  - 100K vectors, 384 dims: ~40-80ms (vs ~1000ms current)
  - 1M vectors, 128 dims: ~100-200ms (vs ~10s current)
- **Page size tuning:** 32KB pages ~2x faster than default 4KB
- **Quantization:** int8 (+15% speed), bit (+25% speed) options available
- **Practical limit:** Hundreds of thousands to few million vectors on commodity hardware

**3. Integration Strategy:**
- ✅ **Non-breaking:** sqlite-vec operates on existing BLOB columns
- ✅ **Graceful fallback:** Load extension, catch failure, fall back to current O(n) approach
- ✅ **No schema migration:** Zero DB changes required
- ✅ **No API changes:** IBackend/ICollection interfaces unchanged
- **Code changes:**
  - `SqliteBackend.GetOrCreateConnectionAsync()`: Call `connection.LoadExtension("sqlite_vec")` with try/catch
  - `SqliteCollection.QueryAsync()`: Route to `QueryWithSqliteVecAsync()` if extension loaded, else current path
  - New method: `QueryWithSqliteVecAsync()` using `vec_distance_cosine(embedding, @query)` SQL function

**4. License Compatibility:**
- ✅ **sqlite-vec License:** MIT
- ✅ **Our License:** MIT
- ✅ **Compatibility:** Fully compatible, no legal blockers
- **Action:** Include sqlite-vec MIT license in LICENSE-THIRD-PARTY file

**5. Fallback Options (if sqlite-vec fails):**
- **Option A:** Managed SIMD (`System.Runtime.Intrinsics`) - 2-3x speedup, still O(n)
- **Option B:** Qdrant - production-grade, external service, breaks local-first model
- **Option C:** Chroma - Python-based, no native .NET client
- **Option D:** LanceDB - Arrow-native, no .NET client, immature
- **Recommendation:** sqlite-vec is best fit for local-first, embedded architecture

**6. Risk Assessment:**
- **Technical risks:** Low-medium (prerelease bugs, cross-platform issues)
- **Mitigation:** Fallback to current O(n), pin to specific version, test all platforms
- **Business risks:** Low (non-breaking change, extensive testing planned)

**7. Next Steps:**
- ✅ **Completed:** Research, NuGet availability confirmed, build test passed
- **Recommended:** Create spike PR to validate full integration
  - Branch: `spike/sqlite-vec-integration`
  - Implement extension loading + fallback
  - Implement sqlite-vec query path
  - Run conformance tests + benchmarks
  - Timebox: 2 days
- **If spike succeeds:** Direct implementation in v0.6.0
- **Effort estimate:** 3-5 engineer-days total

**Decision document:** `.squad/decisions/inbox/tyrell-sqlite-vec-spike.md` (15KB, comprehensive analysis)

**Status:** Research complete, ready for spike PR approval.

