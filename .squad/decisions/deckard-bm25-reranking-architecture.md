# ADR: BM25 and Reranking Integration Architecture

**Status:** PROPOSED  
**Date:** 2026-04-29  
**Lead:** Deckard (Lead Architect)  
**Requested By:** Bruno Capuano  

---

## Problem Statement

MemPalace.Search v0.1 has two limitations:

1. **Keyword Search:** Token-overlap scoring is too naive (no TF-IDF, no corpus stats)
   - Cannot rank phrase queries well
   - No typo tolerance or stemming
   - RRF fusion with weak keyword scores is suboptimal

2. **Reranking:** `IReranker` abstraction is incomplete
   - LLM-only; no ONNX (local) or Ollama support
   - Pass-through implementation (no actual reranking logic)
   - Hard to test or swap backends

ElBruno's new libraries solve both:
- **ElBruno.BM25:** Production-grade keyword search with full BM25 algorithm
- **ElBruno.Reranking:** Pluggable backends (ONNX, Claude, Ollama)

**Question:** How do we integrate these into MemPalace.Search without breaking existing APIs?

---

## Decision

### 1. Introduce BM25SearchService

Create a new service parallel to VectorSearchService:

```csharp
public sealed class Bm25SearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly Bm25Index<Record> _index;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(...)
    {
        // Keyword search via BM25
    }
}
```

**Rationale:**
- Follows existing pattern (VectorSearchService exists)
- Independent of vector path; can be used standalone
- Users opt-in via DI: `AddBm25Search()`

---

### 2. Evolve HybridSearchService to Support All Three

Enhance HybridSearchService to fuse vector + BM25 + reranking:

```csharp
public sealed class HybridSearchService : ISearchService
{
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(...)
    {
        // 1. Vector search
        // 2. BM25 search
        // 3. RRF fusion
        // 4. [Optional] Rerank
    }
}
```

**Rationale:**
- Current HybridSearchService already does fusion; just upgrade keyword component
- RRF formula unchanged; backward compatible output
- Reranking remains optional (SearchOptions.Rerank flag)

---

### 3. Adapter Pattern for Reranking

Create `ElBrunoRerankerAdapter` in MemPalace.Ai.Rerank:

```csharp
public sealed class ElBrunoRerankerAdapter : IReranker
{
    private readonly ElBruno.Reranking.IReranker _inner;

    public async ValueTask<IReadOnlyList<RankedHit>> RerankAsync(...)
    {
        // Convert MemPalace RankedHit ↔ ElBruno RerankItem
        // Call _inner.RerankAsync()
        // Convert results back
    }
}
```

**Rationale:**
- Preserves existing MemPalace.Ai.Rerank.IReranker contract
- No breaking changes for callers
- Allows gradual migration from LlmReranker to ElBrunoRerankerAdapter
- Supports multiple backends (ONNX, Claude, Ollama) transparently

**Alternative Rejected:** Rewrite MemPalace.Ai.Rerank.IReranker to match ElBruno.Reranking.IReranker
- Breaking change for existing code
- Discards proven abstractions (MemPalace.Ai has ship date advantages)

---

### 4. DI Registration Strategy

Add new extension methods to ServiceCollectionExtensions:

```csharp
// In MemPalace.Search
services.AddBm25Search(customTokenizer: null);
services.AddEnhancedHybridSearch(customTokenizer: null);

// In MemPalace.Ai.Rerank
services.AddOnnxReranking(modelPath: "...");
services.AddClaudeReranking(apiKey: "...");
```

**Rationale:**
- Explicit registration; no magic
- Optional: users choose which combination to use
- Backward compatible: `AddMemPalaceSearch()` + `AddHybridSearch()` unchanged

---

### 5. SearchOptions: Keep as-is (v1.0)

**Decision:** Do NOT modify SearchOptions record in v1.0.

```csharp
// Current definition, no changes
public sealed record SearchOptions(
    int TopK = 10,
    string? Wing = null,
    WhereClause? Where = null,
    bool Rerank = false,
    float? MinScore = null);
```

**Rationale:**
- SearchOptions already supports Rerank flag (used by VectorSearchService)
- HybridSearchService will honor Rerank in v1.0+ (currently ignored)
- Custom tokenizers and reranker options can be passed via DI (not per-query)
- Reduces API surface; simpler mental model

**Future (v1.1):** Extend with Mode, CustomTokenizer, RerankConfig if needed.

---

## Implementation Seams

### Seam A: BM25 Index Initialization

**Problem:** Bm25Index<T> is in-memory; must be built from backend data.

**Solution:** Lazy initialization + staleness check

```csharp
public class Bm25SearchService : ISearchService
{
    private readonly Lazy<Bm25Index<Record>> _indexCache;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(...)
    {
        var index = _indexCache.Value; // Lazy init on first access
        
        // Check staleness
        var coll = await _backend.GetCollectionAsync(...);
        if (index.LastUpdated < coll.UpdatedAt)
        {
            var records = await coll.FetchAllAsync(...);
            index.Reindex(records);
        }
        
        // Search
        var results = index.Search(query, topK: opts.TopK * 2);
        // ... convert to SearchHits
    }
}
```

**Trade-off:** Assumes collection size << 1M records. For larger corpora, recommend backend-native FTS.

---

### Seam B: Tokenizer Consistency

**Problem:** BM25 uses ITokenizer (from ElBruno.BM25); semantic embedder is opaque.

**Solution:** Accept ITokenizer dependency in HybridSearchService

```csharp
public class HybridSearchService : ISearchService
{
    private readonly ITokenizer _tokenizer; // NEW

    public HybridSearchService(
        IBackend backend,
        IEmbedder embedder,
        Bm25Index<Record> bm25Index,
        IReranker? reranker,
        ITokenizer? tokenizer = null)
    {
        _tokenizer = tokenizer ?? new SimpleTokenizer();
    }
}
```

**No library changes required;** HybridSearchService passes tokenizer to BM25Index.

---

### Seam C: RRF Fusion with Optional Reranking

**Flow:**

```
Query
  ↓
Vector Search + BM25 Search (parallel)
  ↓
Merge candidates + RRF fusion
  ↓
Top-K results
  ↓
[If Rerank enabled]
  ├─ Convert to RankedHits
  ├─ Call adapter → ElBrunoRerankerAdapter → ElBruno.Reranking.IReranker
  └─ Replace scores with reranker output
  ↓
SearchHits (sorted by final score)
```

**No API changes** to SearchOptions; reranking uses existing Rerank flag.

---

## Known Gaps & Dependencies

### Gap 1: BM25 Index Persistence

**Issue:** ElBruno.BM25 has no save/load API.

**Impact:** Indexes are in-memory only; must rebuild on every app restart.

**Recommendation:** ElBruno.BM25 should add `SaveIndex(path)` and `LoadIndex(path)` methods.

**Workaround (for now):** Rebuild on startup (acceptable for collections < 100K docs).

---

### Gap 2: Reranker Initialization Complexity

**Issue:** ONNX reranker requires model file; Claude requires API key; Ollama requires server URL.

**Impact:** DI setup is backend-specific; no unified factory.

**Recommendation:** Create `IRerankerFactory` with config-driven initialization.

**Workaround (for now):** Explicit DI registration per backend (see "DI Registration Strategy").

---

### Gap 3: Batch Reranking

**Issue:** ElBruno.Reranking.IReranker processes one query at a time.

**Impact:** Reranking 100 candidates = 100 API calls (for Claude backend).

**Impact:** Minor for v1.0 (typical TopK = 10-20); but worth optimizing in v1.1.

**Recommendation:** Add `RerankBatchAsync()` to ElBruno.Reranking.IReranker.

---

## Backward Compatibility

| Use Case | v0.1 Behavior | v1.0 Behavior | Change? |
|----------|---------------|---------------|---------|
| `AddMemPalaceSearch()` | VectorSearchService | VectorSearchService | No |
| `AddHybridSearch()` | Token-overlap hybrid | BM25-based hybrid | No (internal change, same output semantics) |
| `SearchAsync(..., Rerank: true)` | LLM rerank (pass-through) | LLM rerank (functional) | No (same interface, better feature) |
| `new SimpleTokenizer()` | N/A | Available | New |
| Custom BM25 tokenizer | N/A | Via DI | New |
| ONNX/Claude reranking | N/A | Via adapter | New |

**Breaking Changes:** NONE planned.

**Public API Removals:** None (but consider marking internal some v0.1 helpers).

---

## Success Criteria

- [ ] All v0.1 tests pass unchanged
- [ ] BM25SearchService search accuracy improves >30% on benchmark set
- [ ] HybridSearchService produces same output format as v0.1 (SearchHit records)
- [ ] Reranking works with ONNX, Claude, Ollama backends without code changes
- [ ] DI registration is < 5 lines per backend
- [ ] Integration tests cover all three paths (Vector, BM25, Hybrid) with/without reranking
- [ ] Documentation examples are executable end-to-end

---

## Questions for Review

1. **Lazy Index Init:** Should BM25 index rebuild happen on mutation events, or only on staleness check?
2. **Tokenizer Scope:** Should HybridSearchService accept ITokenizer in DI, or pass as SearchOptions?
3. **Reranker Adapter:** Should we also implement `IReranker` directly in ElBrunoRerankerAdapter, or wrap `ElBruno.Reranking.IReranker` only?
4. **v1.1 Roadmap:** Should we add SearchMode enum + RerankOptions to SearchOptions now (for future-proofing)?

---

## Approval Checklist (Deckard)

- [x] Architecture aligns with MemPalace principles (local-first, pluggable)
- [x] No breaking changes to existing public APIs
- [x] Backward compatible with v0.1 deployments
- [x] Implementation plan is feasible within sprint
- [x] Known gaps documented for library maintainers
- [x] Success criteria are measurable
- [ ] Team review + feedback collected
- [ ] Approved by Code Review + Deckard sign-off

---

**Signature:** Deckard, Lead Architect  
**Date:** 2026-04-29  
**Co-authored-by:** Copilot <223556219+Copilot@users.noreply.github.com>
