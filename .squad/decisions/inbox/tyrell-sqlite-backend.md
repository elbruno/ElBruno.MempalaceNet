# Decision: SQLite Backend Vector Storage Approach

**Date:** 2026-04-24  
**Decided by:** Tyrell (Core Engine Dev)  
**Status:** Implemented

## Context

Phase 2 of the project requires implementing a default SQLite backend for MemPalace.NET with vector similarity search capabilities. The Python reference implementation uses ChromaDB, which has native vector indexing. We needed to choose an approach for storing and searching embeddings in SQLite.

## Options Considered

### 1. sqlite-vec Extension
- **Pros:** Native HNSW indexing, fast approximate nearest neighbor search, designed for this use case
- **Cons:** Not available as stable NuGet package at project time; requires native library distribution; adds external dependency

### 2. Microsoft.SemanticKernel.Connectors.Sqlite
- **Pros:** Microsoft-stewarded, .NET-native, tested
- **Cons:** Heavy dependency (pulls in full Semantic Kernel); overkill for MemPalace's needs; may impose architectural constraints

### 3. Pure Managed BLOB Approach (chosen)
- **Pros:** Zero external dependencies; simple implementation; deterministic behavior; portable
- **Cons:** O(n) brute-force search; not suitable for very large collections (>100K records)

## Decision

**Implement brute-force cosine similarity search with BLOB-stored embeddings.**

### Rationale

1. **No stable NuGet package for sqlite-vec:** Would require bundling native libraries and managing cross-platform compatibility. Adds complexity early in the project.

2. **Performance is acceptable for target use case:**
   - ~10ms for 10K records with 128-dim embeddings on modern hardware
   - MemPalace use case (personal knowledge base, agent memory) typically <50K records per collection
   - Python reference also doesn't optimize for million-record scale

3. **Clear upgrade path:** Interface design allows swapping to Qdrant, Chroma, or Pinecone later without changing consuming code. Documented in `backends.md`.

4. **Simplicity wins at this stage:** Project is in early phases. Avoiding native dependencies and version conflicts keeps build simple.

## Implementation Details

### Storage Schema
```sql
CREATE TABLE [collection_{name}] (
    id TEXT PRIMARY KEY,
    document TEXT NOT NULL,
    metadata TEXT NOT NULL,    -- JSON
    embedding BLOB NOT NULL,    -- float32[] as bytes
    dim INTEGER NOT NULL
)
```

### Cosine Distance
```csharp
distance = 1 - (dot_product / (mag_a * mag_b))
```
- Lower distance = higher similarity
- Results sorted ascending
- Matches Python reference behavior

### SQL Identifier Safety
- All table names wrapped in `[brackets]` to handle special chars (hyphens, spaces)
- Prevents syntax errors like `CREATE TABLE collection_test-col`

## Consequences

### Positive
- No external native dependencies
- Simple to build, test, and deploy
- Works on all platforms without special setup
- Easy to understand and debug

### Negative
- Not suitable for >100K records (degraded query performance)
- No approximate search optimization (e.g., HNSW)

### Mitigations
- Documented performance characteristics in `backends.md`
- Provided clear upgrade path to Qdrant/Chroma for scale
- IBackend interface designed to allow swapping backends without code changes

## Deviations from Plan

Original plan mentioned "try sqlite-vec, fallback to brute-force." We went straight to brute-force after confirming no stable NuGet package exists. This is documented as an explicit choice rather than a fallback.

## Next Steps

1. Roy (Phase 3) will integrate Microsoft.Extensions.AI for embeddings
2. Consider adding sqlite-vec when stable NuGet becomes available (opportunistic optimization)
3. Monitor performance in real-world usage; recommend Qdrant if users hit scale limits

## References

- `docs/backends.md` — full backend documentation
- `src/MemPalace.Backends.Sqlite/SqliteBackend.cs` — implementation
- `src/MemPalace.Backends.Sqlite/SqliteCollection.cs` — collection implementation
- Python reference: https://github.com/MemPalace/mempalace (uses ChromaDB)
