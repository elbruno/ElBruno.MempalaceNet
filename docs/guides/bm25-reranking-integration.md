# BM25 and Reranking Integration Architecture

**Status:** Architecture Design (v1.0)  
**Lead Architect:** Deckard  
**Date:** 2026-04-29  
**Scope:** MemPalace.Search integration with ElBruno.BM25 and ElBruno.Reranking

---

## Executive Summary

This document describes the integration architecture for two new NuGet packages into MemPalace.NET's search subsystem:

1. **ElBruno.BM25** — Full-text keyword search via BM25 algorithm (replaces v0.1 token-overlap)
2. **ElBruno.Reranking** — LLM-based semantic reranking (generalizes existing pass-through reranker)

**Key Design Decisions:**
- BM25 integrates as `Bm25SearchService` (parallel to `VectorSearchService`)
- `HybridSearchService` evolves to coordinate all three: vector + BM25 + rerank
- Reranking becomes backend-agnostic (supports ONNX, Claude, Ollama)
- Optional DI registration; backward compatibility maintained

---

## Current State Analysis

### MemPalace.Search Architecture (v0.1)

```
ISearchService (abstraction)
  ├─ VectorSearchService
  │   └─ Uses: IBackend, IEmbedder, [optional] IReranker
  │   └─ Flow: Embed → Vector Query → [optional] Rerank → SearchHits
  │
  └─ HybridSearchService
      └─ Uses: IBackend, IEmbedder (NO reranking)
      └─ Flow: Embed → Vector Query → Token Overlap → RRF Fusion → SearchHits

SearchOptions record:
  - TopK (default: 10)
  - Wing (metadata filter)
  - Where (advanced filter clause)
  - Rerank (bool, ignored by Hybrid)
  - MinScore (threshold filter)

SearchHit record:
  - Id, Document, Score, Metadata
```

**Current Keyword Search Implementation:**
- Naive token overlap: `overlap / max_query_tokens`
- No TF-IDF, no corpus statistics, no stemming
- Performance: O(n) scan, but quick scoring
- Documented as "BM25-lite" in code; actually pre-BM25 baseline

**Current Reranking:**
- `IReranker` abstraction exists (in MemPalace.Ai.Rerank)
- `LlmReranker` wraps IChatClient but is pass-through (TODO)
- `VectorSearchService` calls reranker post-vector-search
- Scores replaced with reranker scores

### Limitations Identified

1. **Keyword Search:** Token overlap insufficient for:
   - Phrase queries ("machine learning")
   - Typo tolerance
   - Term frequency weighting
   - Corpus-aware scoring

2. **Reranking:** No backend abstraction; hardcoded to LLM:
   - No ONNX (local) support
   - No batch reranking
   - No timeout/retry logic
   - Minimal error handling

3. **Hybrid Search:** Cannot leverage proper BM25 scores:
   - Cannot incorporate IDF (corpus stats not available in token overlap)
   - RRF fusion becomes suboptimal with weak keyword scores

4. **Dependency Injection:** No support for swapping or testing rerankers:
   - DI registration assumes single IReranker singleton
   - No factory pattern for multiple backends

---

## Target Architecture

### Layer 1: New Services

#### 1a. Bm25SearchService

**Responsibility:** Keyword search via full BM25 algorithm.

```csharp
public sealed class Bm25SearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly Bm25Index<ICollection.Record> _index;
    private readonly ITokenizer _tokenizer;

    public Bm25SearchService(
        IBackend backend,
        Bm25Index<ICollection.Record> index,
        ITokenizer? tokenizer = null)
    {
        _backend = backend;
        _index = index;
        _tokenizer = tokenizer ?? new SimpleTokenizer();
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        string collection,
        SearchOptions opts,
        CancellationToken ct = default)
    {
        // 1. Load collection from backend
        var coll = await _backend.GetCollectionAsync(...);
        
        // 2. Search via Bm25Index
        var bm25Results = _index.Search(query, topK: opts.TopK * 2); // Overselect
        
        // 3. Apply WhereClause filters (wing, custom where)
        // 4. Convert to SearchHits
        // 5. Return
    }
}
```

**Dependencies:**
- `ElBruno.BM25::Bm25Index<T>` — Core algorithm
- `ElBruno.BM25::ITokenizer` — Pluggable tokenization

**Integration Points:**
- Consumes collection data from `IBackend`
- Produces `SearchHit` records (standard MemPalace output)

---

#### 1b. Evolved HybridSearchService

**Current Responsibility:** Fuse vector + keyword via RRF  
**New Responsibility:** Fuse vector + BM25 + [optional] rerank via RRF

```csharp
public sealed class HybridSearchService : ISearchService
{
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly Bm25Index<Record> _bm25Index;
    private readonly IReranker? _reranker;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        string collection,
        SearchOptions opts,
        CancellationToken ct = default)
    {
        // 1. Vector search (top-2K candidates)
        var vectorResults = await VectorSearchAsync(query, collection, opts, ct);
        
        // 2. BM25 keyword search (top-2K candidates)
        var bm25Results = await Bm25SearchAsync(query, collection, opts, ct);
        
        // 3. RRF fusion on both signal sets
        var fusedResults = FuseViaRrf(vectorResults, bm25Results, opts.TopK);
        
        // 4. [Optional] Rerank if requested
        if (opts.Rerank && _reranker != null)
        {
            fusedResults = await RerankResultsAsync(query, fusedResults, opts, ct);
        }
        
        // 5. Apply MinScore threshold, return
        return ApplyThreshold(fusedResults, opts.MinScore);
    }
}
```

**Design Rationale:**
- Keeps RRF fusion logic (proven in v0.1)
- Replaces token-overlap with real BM25 scores
- Reranking now optional for hybrid (improves precision if available)
- Backward compatible: existing callers see no difference

---

### Layer 2: Reranker Abstraction (Generalized)

#### 2a. ElBruno.Reranking Integration

**Current MemPalace.Ai.Rerank.IReranker:**
```csharp
public interface IReranker
{
    ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default);
}
```

**ElBruno.Reranking.IReranker:**
```csharp
public interface IReranker
{
    Task<RerankResult> RerankAsync(
        string query,
        IEnumerable<RerankItem> items,
        RerankOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**Adapter Strategy (Option A: Recommended)**

Create a thin adapter in `MemPalace.Ai.Rerank`:

```csharp
namespace MemPalace.Ai.Rerank;

using ElBruno.Reranking;
using System.Collections.Generic;

/// <summary>
/// Adapter bridging MemPalace.Ai.Rerank.IReranker to ElBruno.Reranking.IReranker.
/// Allows use of ONNX, Claude, Ollama backends transparently.
/// </summary>
public sealed class ElBrunoRerankerAdapter : IReranker
{
    private readonly ElBruno.Reranking.IReranker _innerReranker;
    private readonly bool _ownInnerReranker;

    public ElBrunoRerankerAdapter(ElBruno.Reranking.IReranker innerReranker)
    {
        _innerReranker = innerReranker ?? throw new ArgumentNullException(nameof(innerReranker));
        _ownInnerReranker = false;
    }

    public async ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default)
    {
        if (candidates == null || candidates.Count == 0)
            return Array.Empty<RankedHit>();

        // Convert to ElBruno.Reranking items
        var items = candidates
            .Select(rh => new RerankItem { Id = rh.Id, Text = rh.Document })
            .ToList();

        // Call ElBruno reranker
        var result = await _innerReranker.RerankAsync(
            query, items,
            new RerankOptions { TopK = candidates.Count },
            ct);

        // Convert back to MemPalace RankedHits
        return result.Scores
            .Select(rs => new RankedHit(rs.Item.Id, rs.Item.Text, rs.Score))
            .ToList();
    }
}
```

**Why This Approach:**
- Maintains existing MemPalace.Ai.Rerank.IReranker contract
- Zero breaking changes for existing code
- Allows gradual migration to ElBruno.Reranking
- Supports multiple backends (ONNX, Claude, Ollama) via adapter

---

### Layer 3: Dependency Injection

#### 3a. New Extension Methods

**In `MemPalace.Search.ServiceCollectionExtensions`:**

```csharp
/// <summary>
/// Registers Bm25SearchService with optional custom tokenizer.
/// </summary>
public static IServiceCollection AddBm25Search(
    this IServiceCollection services,
    ITokenizer? customTokenizer = null)
{
    services.AddSingleton<ISearchService>(sp =>
    {
        var backend = sp.GetRequiredService<IBackend>();
        var embedder = sp.GetRequiredService<IEmbedder>();
        var tokenizer = customTokenizer ?? new SimpleTokenizer();
        
        // Initialize BM25 index from collection (async, but call sync for DI)
        // TODO: Clarify how to init index at DI time vs. first search
        var index = new Bm25Index<Record>(...);
        
        return new Bm25SearchService(backend, index, tokenizer);
    });
    
    return services;
}

/// <summary>
/// Registers enhanced HybridSearchService with BM25 + Vector + optional Rerank.
/// </summary>
public static IServiceCollection AddEnhancedHybridSearch(
    this IServiceCollection services,
    ITokenizer? customTokenizer = null)
{
    services.AddSingleton<ISearchService>(sp =>
    {
        var backend = sp.GetRequiredService<IBackend>();
        var embedder = sp.GetRequiredService<IEmbedder>();
        var reranker = sp.GetService<IReranker>(); // Optional
        var tokenizer = customTokenizer ?? new SimpleTokenizer();
        
        var bm25Index = new Bm25Index<Record>(...);
        return new HybridSearchService(backend, embedder, bm25Index, reranker, tokenizer);
    });
    
    return services;
}
```

**In `MemPalace.Ai.Rerank.ServiceCollectionExtensions`:**

```csharp
/// <summary>
/// Registers ElBruno ONNX reranker as IReranker.
/// </summary>
public static IServiceCollection AddOnnxReranking(
    this IServiceCollection services,
    string modelPath)
{
    services.AddSingleton<IReranker>(sp =>
    {
        var onnxReranker = new ElBruno.Reranking.OnnxReranker(modelPath);
        return new ElBrunoRerankerAdapter(onnxReranker);
    });
    
    return services;
}

/// <summary>
/// Registers ElBruno Claude reranker as IReranker.
/// </summary>
public static IServiceCollection AddClaudeReranking(
    this IServiceCollection services,
    string apiKey)
{
    services.AddSingleton<IReranker>(sp =>
    {
        var claudeReranker = new ElBruno.Reranking.ClaudeReranker(apiKey);
        return new ElBrunoRerankerAdapter(claudeReranker);
    });
    
    return services;
}
```

---

## Integration Seams & Flow Diagrams

### Seam 1: Collection Data → BM25 Index

**Problem:** BM25 index must be built from backend collection data (documents + metadata).

**Solutions Considered:**

| Approach | Pros | Cons |
|----------|------|------|
| **Lazy Load (first search)** | No upfront cost, automatic | First query slower, concurrent builds clash |
| **Async Factory** | Explicit, testable | Requires DI refactor, complex error handling |
| **Pre-built index (offline)** | Fastest, predictable | Manual rebuild on collection updates, coordination |
| **Materialization on Store** | In-sync with data | Adds latency to store operations |

**Recommended: Lazy Load + Thread-Safe Cache**

```csharp
private readonly Lazy<Bm25Index<Record>> _indexCache;

public async Task<IReadOnlyList<SearchHit>> SearchAsync(...)
{
    var index = _indexCache.Value; // Lazy initializes on first access
    
    // Fetch records from collection
    var records = await _backend.FetchAllAsync(...);
    
    // Rebuild index if stale (tracking via collection metadata timestamp)
    if (index.LastUpdated < collection.UpdatedAt)
    {
        index.Reindex(records);
    }
    
    // Search
    var results = index.Search(query, ...);
    // ...
}
```

**Trade-off:** Assumes collection size is manageable (typical: <100K docs). For larger corpora, recommend backend-native FTS (SQLite FTS5, Qdrant full-text filters).

---

### Seam 2: Hybrid Fusion with Optional Reranking

**Flow:**

```
User Query
    ↓
[Hybrid Service]
    ├─ Parallel Searches:
    │  ├─ Vector: Embed query → KNN on backend
    │  └─ BM25: Tokenize query → Index search
    ├─ Merge Candidates (2K vectors + 2K BM25 = 4K pool)
    ├─ RRF Fusion: Score = 1/(60+vector_rank) + 1/(60+bm25_rank)
    ├─ Top-K Results (truncate at K)
    │
    └─ [If Rerank Enabled]
       ├─ Convert Top-K to RerankItems
       ├─ Call IReranker (ONNX/Claude/Ollama)
       └─ Replace scores with reranker output
    
    ↓
SearchHits (sorted by final score)
```

**Reranking Integration:**

```csharp
if (opts.Rerank && _reranker != null)
{
    var rankedHits = fusedResults.Select(sh => 
        new RankedHit(sh.Id, sh.Document, sh.Score)).ToList();
    
    var reranked = await _reranker.RerankAsync(query, rankedHits, ct);
    
    // Update scores in SearchHits
    var rerankMap = reranked.ToDictionary(rh => rh.Id, rh => rh.Score);
    return fusedResults
        .Select(sh => sh with { Score = rerankMap[sh.Id] })
        .OrderByDescending(sh => sh.Score)
        .ToList();
}
```

---

### Seam 3: Metadata Annotations

**Current:** Hybrid service adds `sources: ["vector", "keyword"]` to metadata.  
**New:** Include all three sources when available.

```csharp
var metadata = new Dictionary<string, object?>(hit.Metadata ?? new())
{
    ["sources"] = new[] { "vector", "bm25", "rerank" }
        .Where(s => /* source contributed */)
        .ToArray(),
    ["vector_rank"] = vectorRank,
    ["bm25_rank"] = bm25Rank,
    ["bm25_score"] = bm25Score,
    ["rerank_score"] = opts.Rerank ? rerankScore : null
};
```

---

## SearchOptions Enhancements

### Current Definition
```csharp
public sealed record SearchOptions(
    int TopK = 10,
    string? Wing = null,
    WhereClause? Where = null,
    bool Rerank = false,
    float? MinScore = null);
```

### Proposed Extension

**Option A: Additive (Backward Compatible)**
```csharp
public sealed record SearchOptions(
    int TopK = 10,
    string? Wing = null,
    WhereClause? Where = null,
    bool Rerank = false,
    float? MinScore = null,
    SearchMode Mode = SearchMode.Default,        // NEW
    ITokenizer? CustomTokenizer = null,          // NEW
    RerankOptions? RerankConfig = null);         // NEW

public enum SearchMode
{
    Default,           // Auto-select (Vector, or Hybrid if available)
    VectorOnly,
    BM25Only,
    Hybrid
}
```

**Benefits:**
- Users can explicitly choose strategy per-query
- Custom tokenizer support without DI refactor
- Reranker options passed through (timeout, backend, etc.)

**Risks:**
- API surface grows; more decision points for users

**Alternative (Option B: Keep as-is)**
- Users must register desired service at DI time
- Simpler API; less runtime flexibility
- Recommended for v1.0 (keep Option A for v1.1)

---

## Known Gaps & Library Dependencies

### Gap 1: BM25 Index Lifecycle

**Issue:** `Bm25Index<T>` is stateful; unclear how to manage across service calls.

**Current ElBruno.BM25 API:**
```csharp
public class Bm25Index<T>
{
    public Bm25Index(IEnumerable<T> documents, Func<T, string> textExtractor);
    public List<(T Document, float Score)> Search(string query, int topK);
    public void AddDocument(T document);
    public void RemoveDocument(T document);
    public void Reindex(IEnumerable<T> documents);
}
```

**Gap:** No built-in persistence (save/load). Index is in-memory only.

**MemPalace Workaround:**
- Build index on first search
- Cache via Lazy<T> or ScopedCache
- Invalidate on collection mutation (detected via timestamp or event)
- For persistence, serialize BM25 internals to backend metadata (future work)

**Recommendation for ElBruno.BM25:**
- Add `SaveIndex(string path)` and `LoadIndex(string path)` methods
- Enable MemPalace to persist indexes to disk alongside collections

---

### Gap 2: BM25 Tokenizer Inconsistency

**Issue:** MemPalace uses `IEmbedder` (for semantic); BM25 uses `ITokenizer`.

**Current Situation:**
- `ElBruno.BM25.ITokenizer` has `Tokenize(text): List<string>` + `Normalize(term): string`
- MemPalace.Ai has no tokenizer abstraction (embedders opaque)
- Hybrid service currently has hardcoded tokenization (split + filter)

**Risk:** Keyword and semantic tokenization may diverge, causing misalignment.

**Recommendation:**
- MemPalace should standardize on `ElBruno.BM25.ITokenizer` for keyword operations
- Extend `VectorSearchService` to accept optional `ITokenizer` (for term extraction in metadata)
- Update `HybridSearchService` to use `ITokenizer` instead of hardcoded split

**No library change needed**, but requires MemPalace refactor.

---

### Gap 3: Reranker Backend Initialization

**Issue:** ElBruno.Reranking backends require model files or API keys at construction.

**Backends:**
1. **ONNX:** Requires `.onnx` model file path (e.g., `bge-reranker-base.onnx`)
2. **Claude:** Requires Anthropic API key
3. **Ollama:** Requires Ollama server URL + model name

**MemPalace Gap:** No abstraction over backend configuration.

**Recommendation:**
- Add `RerankerOptions` to `SearchOptions` (see Gap 1 solution)
- Or: Create `IRerankerFactory` with config-driven initialization
- Example:
  ```csharp
  var factory = new RerankerFactory(
      new OnnxBackendConfig { ModelPath = "~/models/bge-reranker-base.onnx" }
  );
  var reranker = factory.CreateReranker();
  ```

**No library change needed**, but requires MemPalace DI enhancement.

---

### Gap 4: Batch Reranking

**Issue:** Current `IReranker.RerankAsync()` processes one query at a time.

**Use Case:** In HybridSearchService, reranking top-100 results is O(100 API calls) if not batched.

**ElBruno.Reranking Status:** No batch API defined.

**MemPalace Workaround:**
- Call single `RerankAsync()` with all candidates at once
- Most backends (ONNX, Claude, Ollama) support scoring multiple docs per query in one call
- Adapter may need to extract and re-wrap batched results

**Recommendation for ElBruno.Reranking:**
- Add `RerankBatchAsync()` method to `IReranker` (optional)
- Allows backends to optimize for large candidate sets

---

## Backward Compatibility

### v0.1 Behavior Preservation

| Scenario | Current | v1.0 (BM25/Rerank) | Notes |
|----------|---------|-------------------|-------|
| `AddMemPalaceSearch()` | VectorSearchService | VectorSearchService | No change |
| `AddHybridSearch()` | Token-overlap hybrid | BM25-based hybrid | Improved keyword scoring |
| `Rerank: true` | LLM rerank (passthrough) | LLM rerank (actual) | Enhanced feature |
| Custom tokenizer | N/A | Via DI or SearchOptions | New capability |

**Breaking Changes:** None planned for v1.0.

**Deprecations:**
- Consider marking `HybridSearchService.Tokenize()` private (was public in v0.1)
- Replace with `ITokenizer` dependency

---

## Implementation Roadmap

### Phase 1: Foundation (Sprint 1)
- [ ] Create `Bm25SearchService` with lazy index init
- [ ] Update `HybridSearchService` to use BM25 + RRF
- [ ] Add `AddBm25Search()` and `AddEnhancedHybridSearch()` DI methods
- [ ] Update `SearchOptions` documentation

### Phase 2: Reranking Generalization (Sprint 2)
- [ ] Create `ElBrunoRerankerAdapter`
- [ ] Add `AddOnnxReranking()`, `AddClaudeReranking()` DI methods
- [ ] Update `VectorSearchService` reranking logic
- [ ] Update `HybridSearchService` reranking support

### Phase 3: Testing & Docs (Sprint 3)
- [ ] Integration tests (BM25 index init, search accuracy)
- [ ] Reranking tests (adapter, multiple backends)
- [ ] Performance benchmarks (vector vs BM25 vs hybrid)
- [ ] User guide in `docs/guides/bm25-reranking-integration.md`

### Phase 4: Future Enhancements
- [ ] BM25 index persistence (disk serialization)
- [ ] Backend-native FTS (SQLite FTS5, Qdrant)
- [ ] Batch reranking API
- [ ] QueryExpansion (synonym injection)
- [ ] Reranker selection per-query

---

## Diagram: High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      MemPalace.Search                        │
├──────────────────────────────────────────────────────────────┤
│                     ISearchService                           │
│                                                              │
├─────────────────┬──────────────────┬──────────────────────┤
│ Vector Search   │ BM25 Search      │ Hybrid Search (NEW)  │
│                 │                  │                      │
│ • Embed query   │ • Tokenize query │ • All three above:   │
│ • KNN lookup    │ • BM25 index     │   - Vector search    │
│ • [Rerank?]     │ • Scored results │   - BM25 search      │
│                 │ • [Rerank?] (v2) │   - RRF fusion       │
│                 │                  │   - [Rerank?]        │
├─────────────────┴──────────────────┴──────────────────────┤
│                                                              │
│  Services (Injected)                                         │
│  • IBackend (collection queries)                             │
│  • IEmbedder (semantic embeddings)                           │
│  • ITokenizer (BM25 tokenization)                            │
│  • IReranker (optional, multiple backends)                   │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    ElBruno Libraries                         │
│  ┌────────────────┬──────────────────────────────────────┐  │
│  │ ElBruno.BM25   │ ElBruno.Reranking                    │  │
│  │                │                                      │  │
│  │ • Bm25Index<T> │ • IReranker                          │  │
│  │ • ITokenizer   │ • OnnxReranker (ONNX)                │  │
│  │ • Bm25Tuner    │ • ClaudeReranker (Claude API)        │  │
│  │                │ • OllamaReranker (Local LLM)         │  │
│  │                │ • RerankOptions, RerankItem         │  │
│  └────────────────┴──────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| BM25 index OOM on large collections | Medium | High | Lazy load + warn on size; recommend backend FTS |
| Tokenizer mismatch (semantic vs keyword) | Medium | Medium | Unify on ElBruno.BM25.ITokenizer |
| Reranker initialization complexity | Low | Medium | Factory pattern + clear DI docs |
| API proliferation (too many search modes) | Medium | Low | Keep v1.0 simple; add options in v1.1 |
| Backward compatibility break | Low | High | Strict testing; feature flags if needed |

---

## Success Criteria

- [ ] All existing tests pass (VectorSearchService, HybridSearchService)
- [ ] BM25SearchService outperforms token-overlap by 30%+ on benchmark queries
- [ ] HybridSearchService RRF fusion balances vector+BM25 with ±10% variance
- [ ] Reranking works with ONNX, Claude, and Ollama backends via adapter
- [ ] DI registration is simple (2-3 lines of code)
- [ ] Documentation updated and examples run
- [ ] Integration tests cover all three (Vector, BM25, Hybrid) with and without reranking

---

## References

- **ElBruno.BM25:** https://github.com/elbruno/ElBruno.BM25
- **ElBruno.Reranking:** https://github.com/elbruno/ElBruno.Reranking
- **MemPalace.Search:** `docs/search.md`
- **Architecture Decisions:** `.squad/decisions/deckard-bm25-reranking-architecture.md`
