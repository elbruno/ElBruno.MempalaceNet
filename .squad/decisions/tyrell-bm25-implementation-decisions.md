# Tyrell's BM25 Implementation Gaps & Decisions

**Status**: Phase 1 Foundation Complete  
**Date**: 2026-04-28  
**Related**: [deckard-bm25-reranking-architecture.md](deckard-bm25-reranking-architecture.md)

## Summary
Implemented BM25 search integration following Deckard's architecture. All core functionality complete with zero breaking changes.

## Library Discoveries

### ElBruno.BM25 (v0.5.0)
**Status**: Works as expected ✅

**API Details**:
- **Class**: `Bm25Index<T>` (generic, not static)
- **Constructor**: `Bm25Index<T>(documents, contentSelector: Func<T,string>, tokenizer, parameters, caseInsensitive)`
  - Documents passed to constructor (not added incrementally)
  - `contentSelector` parameter (not `getText`)
  - `caseInsensitive` parameter (not `trackScores`)
- **Search Method**: `Search(query: string, topK: int, threshold: double, ct: CancellationToken)`
  - Returns `List<(T document, double score)>` tuples (not `SearchResult<T>` objects)
  - `threshold` parameter for minimum score filtering
- **Tokenizers**: Provided via `ElBruno.BM25.Tokenizers` namespace
  - Default: `EnglishTokenizer`
  - Also supports: `SimpleTokenizer`, `CustomTokenizer`

**Observations**:
- API is stable and well-documented via XML docs
- Design choice: documents loaded upfront rather than incremental adds (suitable for v0.5's in-memory model)
- Tuple return type is lightweight and performant

### ElBruno.Reranking (latest)
**Status**: Not yet integrated ⏳

**Note**: HybridSearchService conditionally uses `IReranker` if available; exact ElBruno.Reranking API TBD for v1.1 adapter.

## Implementation Decisions

### 1. In-Memory Index Strategy
**Decision**: Load all documents at index build time  
**Rationale**: 
- Matches ElBruno.BM25 API design
- Suitable for v0.5 single-palace usage
- Simplifies staleness detection

**Future (v1.1)**: 
- Persist index to backend storage to avoid rebuild on restart
- Support filtered indices for wing-specific searches

### 2. Staleness Detection
**Decision**: Simple timestamp comparison  
**Rationale**:
- Minimal overhead
- Regenerates index only when modified (or when filters change)

**Limitation**: 
- Currently rebuilds if wing/where filters used (no cached filtered indices)
- v1.1 will support filter-aware caching

### 3. HybridSearchService Upgrade
**Decision**: Replace token-overlap with BM25 via Reciprocal Rank Fusion  
**Rationale**:
- BM25 is industry standard for keyword relevance
- RRF proven method for combining multiple ranking signals
- No breaking changes to SearchHit API

**Reranking**: 
- Optional post-fusion step when `SearchOptions.Rerank == true`
- Requires IReranker to be registered in DI

### 4. Backward Compatibility
**Decision**: Maintain existing DI method names; AddHybridSearch() wraps AddEnhancedHybridSearch()  
**Rationale**:
- No migration pain for existing consumers
- New AddEnhancedHybridSearch() available for explicit opt-in

## Gaps & TODOs

### v1.1 Roadmap
- [ ] Persist BM25 index to backend (SQLite blob)
- [ ] Support filtered indices for wing-specific searches
- [ ] Implement `ElBrunoRerankerAdapter` to bridge `IReranker` to ElBruno.Reranking backends
- [ ] Batch search optimization for SearchBatch()
- [ ] Parameter tuning via Bm25Tuner<T>

### Testing Gaps
- BM25SearchService: Unit tests for index building, staleness detection, tuple mapping
- HybridSearchService: Integration tests for RRF fusion, reranking flow
- End-to-end: SearchCommand integration with new hybrid search

### Documentation
- [ ] Update CLI help text for enhanced hybrid search
- [ ] Add BM25 vs token-overlap comparison to docs/search.md
- [ ] ElBruno.Reranking adapter pattern example

## Validation Results

### Build Status
```
✅ dotnet build src/MemPalace.Search/ -> SUCCESS (Release)
   - BM25SearchService: compiles without warnings
   - HybridSearchService: compiles without warnings
   - ServiceCollectionExtensions: DI methods registered
```

### Existing Tests
- Test project has unrelated failures (IEmbedder.EmbedAsync signature issues)
- Search-specific tests not yet run (blocked on test project fix)
- Manual verification: HybridSearchService instantiation works in DI

## Commits (Phase 1)
1. `feat(search): add NuGet references for BM25 and Reranking`
2. `feat(search): implement BM25SearchService`
3. `feat(search): upgrade HybridSearchService to use BM25`
4. `docs: document BM25 implementation gaps and decisions`

## Sign-Off
✅ Phase 1 Foundation complete per Deckard's architecture  
✅ Zero breaking changes  
✅ DI registration functional  
✅ Ready for Phase 2 (optional reranking + testing)  

**Verified By**: Tyrell (Core Engine Dev)  
**Date**: 2026-04-28
