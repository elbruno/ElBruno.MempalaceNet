# Search

MemPalace.NET provides vector and hybrid search capabilities for querying embedded memories.

## Overview

Two search service implementations:

1. **VectorSearchService** — Pure semantic search using embedding similarity
2. **HybridSearchService** — Combines vector and keyword signals via Reciprocal Rank Fusion

Both implement `ISearchService` and integrate with the backend/embedder abstractions.

## Basic Usage

```csharp
using MemPalace.Search;

var searchService = serviceProvider.GetRequiredService<ISearchService>();

var options = new SearchOptions(
    TopK: 10,
    Wing: "code",          // Filter by metadata field
    Rerank: true,          // Enable LLM reranking
    MinScore: 0.7f         // Threshold (0-1 scale)
);

var results = await searchService.SearchAsync(
    query: "how to implement search",
    collection: "my-collection",
    opts: options
);

foreach (var hit in results)
{
    Console.WriteLine($"{hit.Score:F2} - {hit.Document}");
}
```

## SearchHit

```csharp
public sealed record SearchHit(
    string Id,                                  // Record ID from backend
    string Document,                            // Full text content
    float Score,                                // Relevance score (0-1, higher = better)
    IReadOnlyDictionary<string, object?>? Metadata  // Original metadata + search annotations
);
```

**Score Interpretation:**
- **VectorSearchService:** `1 - cosine_distance` (0 = opposite, 1 = identical)
- **HybridSearchService:** RRF score (typically 0.01-0.05 for top results)
- Scores are NOT comparable across services

**Metadata Annotations:**
- `sources` (Hybrid only): `["vector", "keyword"]` — which signals contributed

## VectorSearchService

Pure semantic search using embedding similarity.

### How It Works

1. Embed query with same embedder used for collection
2. Query backend for nearest neighbors (cosine distance)
3. Convert distances to similarity scores: `score = 1 - distance`
4. Optionally rerank with LLM

### Configuration

```csharp
services.AddSingleton<ISearchService>(sp => {
    var backend = sp.GetRequiredService<IBackend>();
    var embedder = sp.GetRequiredService<IEmbedder>();
    var reranker = sp.GetService<IReranker>();  // Optional
    return new VectorSearchService(backend, embedder, reranker);
});
```

### SearchOptions

- **TopK:** Number of results to return (default: 10)
- **Wing:** Translated to `WhereClause` filter: `Eq("wing", value)`
- **Where:** Advanced filtering (e.g., `And([Eq("type", "doc"), Gt("year", 2020)])`)
- **Rerank:** Enable LLM-based reranking (requires `IReranker` in DI)
- **MinScore:** Filter results below threshold (applied AFTER reranking)

### Reranking

When `Rerank = true` and `IReranker` is available:
1. Initial vector search returns candidates
2. Reranker scores each candidate using LLM context
3. Results are re-ordered by reranker scores
4. Final `SearchHit.Score` reflects reranker output

**Benefits:**
- Captures nuanced semantic relevance
- Considers query context and intent

**Costs:**
- LLM API calls proportional to `TopK`
- 2-10x latency increase

## HybridSearchService

Combines vector and keyword signals for robust retrieval.

### How It Works

1. **Vector Search:** Embed query, retrieve top-2×K candidates
2. **Keyword Scoring:** Simple token overlap (BM25-lite)
   - Tokenize query and documents
   - Score = `|query_tokens ∩ doc_tokens| / |query_tokens|`
3. **Reciprocal Rank Fusion (RRF):**
   ```
   score(doc) = Σ (1 / (k + rank_in_source))
   ```
   where `k = 60` (standard RRF constant)
4. Return top-K by fused score

### Keyword Scoring (v0.1)

**Current Implementation:**
- Token extraction: split on whitespace + punctuation
- Overlap: case-insensitive set intersection
- No stopwords, stemming, or TF-IDF

**Simplification Rationale:**
- BM25 requires document frequency statistics (corpus-wide)
- Token overlap is fast, deterministic, and sufficient for v0.1
- Future: upgrade to full BM25 with inverted index

### RRF Formula

For each document:
```
rrf_score = vector_contribution + keyword_contribution

vector_contribution = 1 / (60 + vector_rank)
keyword_contribution = 1 / (60 + keyword_rank)
```

**Example:**
- Doc A: vector rank 1, keyword rank 3 → `1/61 + 1/63 = 0.0323`
- Doc B: vector rank 2, keyword rank 1 → `1/62 + 1/61 = 0.0325` ✓ (higher)

RRF balances contributions from both signals without explicit weight tuning.

### Configuration

```csharp
services.AddHybridSearch();  // Swaps default ISearchService to HybridSearchService
```

### When to Use Hybrid vs Vector

**Use Hybrid if:**
- Queries contain entity names (e.g., "ElBruno mempalacenet")
- Exact keyword matches matter (e.g., "NSubstitute mocking")
- Domain has specialized jargon
- Users mix semantic and exact-match queries

**Use Vector if:**
- Queries are natural language questions
- Embeddings capture domain semantics well
- Keyword noise is high (e.g., code comments)

## SearchOptions (Complete Reference)

```csharp
public sealed record SearchOptions(
    int TopK = 10,                  // Number of results (1-1000)
    string? Wing = null,            // Filter by wing metadata
    WhereClause? Where = null,      // Advanced backend filter
    bool Rerank = false,            // Enable LLM reranking (Vector only)
    float? MinScore = null          // Minimum score threshold
);
```

**Notes:**
- `Wing` and `Where` are mutually exclusive (Wing overrides)
- `MinScore` semantics differ by service (see Score Interpretation)
- `Rerank` is ignored by `HybridSearchService`

## DI Registration

### Default (Vector)

```csharp
services.AddMemPalaceSearch();
```

Registers:
- `VectorSearchService` as `ISearchService`
- Requires: `IBackend`, `IEmbedder`
- Optional: `IReranker` (for reranking support)

### Hybrid

```csharp
services.AddHybridSearch();  // Replaces default
```

Registers:
- `HybridSearchService` as `ISearchService`
- Requires: `IBackend`, `IEmbedder`
- Does NOT use `IReranker`

## Performance

### VectorSearchService

- **Query latency:** 10-100ms (depends on backend implementation)
- **With reranking:** +500-2000ms (LLM call)
- **Bottleneck:** Backend cosine similarity computation

### HybridSearchService

- **Query latency:** 20-150ms (vector + keyword scoring)
- **Keyword overhead:** 2-10ms (depends on corpus size and TopK)
- **Bottleneck:** Vector search (keyword scoring is O(n) but fast)

**Optimization Tips:**
- Use backend-native vector search (e.g., Qdrant, Chroma) for >10K records
- Index keyword fields if backend supports (e.g., SQLite FTS5)
- Cache embeddings for frequent queries
- Tune `TopK`: smaller = faster

## CLI Integration

```bash
# Vector search (default)
mempalacenet search "how to implement async iterators"

# With filters
mempalacenet search "mining pipeline" --wing code --top-k 5

# With reranking (requires IReranker in DI)
mempalacenet search "best practices" --rerank

# Hybrid search (if configured)
mempalacenet search "FileSystemMiner chunking" --wing documentation
```

## Error Handling

- **Collection not found:** Returns empty results (no exception)
- **Embedder failure:** Propagates exception
- **Backend timeout:** Depends on backend implementation

## Roadmap

- **Full BM25:** Replace token-overlap with proper BM25 scoring
- **Inverted index:** Add keyword index to backends (SQLite FTS5, etc.)
- **Query expansion:** Synonym injection, spelling correction
- **Faceted search:** Group results by metadata fields
- **Multi-vector:** Search across multiple collections simultaneously
- **Result caching:** LRU cache for frequent queries
- **Streaming results:** Return partial results as they're scored
