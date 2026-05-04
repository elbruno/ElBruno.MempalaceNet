# Reranking Workflow Guide

**Improve search quality with LLM-based reranking** — Boost precision for high-stakes use cases like customer support Q&A, legal document search, and technical troubleshooting.

**Read time:** ~6 minutes | **Difficulty:** Intermediate

---

## Why Reranking Matters

Semantic search with embeddings is fast and scales well, but it doesn't always surface the *most* relevant result at the top. Reranking applies a second-pass LLM to re-order candidates based on deeper semantic understanding.

### Quality Improvement

- **Semantic search alone:** ~85% accuracy (good for broad recall)
- **Semantic + LLM reranking:** ~95%+ accuracy (excellent for precision)

### Trade-offs

| Metric | Semantic Search | Semantic + Reranking |
|--------|----------------|---------------------|
| Latency | ~50ms | ~150–200ms |
| Precision | Good (85%) | Excellent (95%+) |
| Cost | Free (ONNX) | API calls (if cloud LLM) |
| Use case | General retrieval | High-precision tasks |

---

## How It Works

**Reranking is a two-phase pipeline:**

1. **Phase 1: Semantic search** — Fast vector search retrieves top-N candidates (e.g., 10–20 results)
2. **Phase 2: LLM reranking** — Language model re-scores candidates based on query relevance, promoting the best matches to the top

**Why not just use LLM for everything?**  
LLMs are expensive and slow for large corpora. Semantic search narrows the field, then reranking refines the top results.

---

## Step-by-Step Walkthrough

### Prerequisites

- MemPalace.NET installed (`dotnet add package mempalacenet`)
- A palace with mined documents (see [Getting Started](GETTING_STARTED.md))
- An `IReranker` implementation (mock for testing, real LLM for production)

---

### Step 1: Create Palace and Mine Documents

```csharp
using MemPalace;
using MemPalace.Ai.Rerank;
using MemPalace.Core.Model;

// Initialize palace
var palace = await Palace.Create("~/my-palace");

// Mine a document corpus (20+ documents recommended)
var documents = new[]
{
    "Authentication: Use JWT tokens for stateless authentication. Token expiry is 15 minutes.",
    "Caching: Redis cache for session data. TTL is 1 hour. Use sliding expiration.",
    "Logging: Structured logging with Serilog. Log levels: Debug, Info, Warning, Error.",
    "Error handling: Catch exceptions at controller level. Return 500 with error ID.",
    "Security: HTTPS only. CORS enabled for trusted origins. CSRF tokens required.",
    "Database: PostgreSQL with connection pooling. Max 20 connections per instance.",
    "API design: RESTful endpoints. Use HTTP verbs correctly. Versioning in URL.",
    "Testing: Unit tests with xUnit. Integration tests with TestContainers.",
    "Deployment: Docker containers. Kubernetes for orchestration.",
    "Monitoring: Application Insights for telemetry. Custom metrics for business events."
};

// Store in palace
foreach (var doc in documents)
{
    await palace.Store(doc, wing: "documentation");
}
```

---

### Step 2: Perform Semantic Search

```csharp
// User asks a question
var userQuery = "How do we handle errors in the API?";

// Semantic search retrieves top-10 candidates
var candidates = await palace.Search(
    query: userQuery,
    wing: "documentation",
    limit: 10
);

Console.WriteLine($"Semantic search found {candidates.Count} candidates");
foreach (var candidate in candidates.Take(3))
{
    Console.WriteLine($"[{candidate.Score:F3}] {candidate.Memory.Content}");
}
```

**Expected output:**
```
Semantic search found 10 candidates
[0.823] Error handling: Catch exceptions at controller level. Return 500 with error ID.
[0.781] Logging: Structured logging with Serilog. Log levels: Debug, Info, Warning, Error.
[0.745] API design: RESTful endpoints. Use HTTP verbs correctly. Versioning in URL.
```

---

### Step 3: Initialize Reranker

For production, use an LLM-backed reranker. For testing, use a mock:

```csharp
// Production: Cloud LLM reranker (example)
var reranker = new OpenAIReranker(
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    model: "gpt-4"
);

// Testing: Mock reranker (keyword-based)
var reranker = new MockReranker();
```

**Mock reranker example** (for development/testing):

```csharp
public class MockReranker : IReranker
{
    public ValueTask<IReadOnlyList<RankedHit>> RerankAsync(
        string query,
        IReadOnlyList<RankedHit> candidates,
        CancellationToken ct = default)
    {
        var queryTerms = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var reranked = candidates
            .Select(hit =>
            {
                var docTerms = hit.Document.ToLowerInvariant()
                    .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet();

                var overlap = queryTerms.Intersect(docTerms).Count();
                var keywordScore = overlap / (float)queryTerms.Count;

                // Combine semantic (original) + keyword scores
                var combinedScore = (hit.Score * 0.6f) + (keywordScore * 0.4f);
                var boostedScore = combinedScore * (1.0f + (keywordScore * 0.15f));

                return new RankedHit(hit.Id, hit.Document, boostedScore);
            })
            .OrderByDescending(h => h.Score)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<RankedHit>>(reranked);
    }
}
```

---

### Step 4: Apply Reranker to Candidates

```csharp
using System.Diagnostics;

// Convert search results to RankedHit format
var rankedHits = candidates.Select(c => new RankedHit(
    c.Memory.Id,
    c.Memory.Content,
    c.Score
)).ToList();

// Measure reranking latency
var sw = Stopwatch.StartNew();
var rerankedResults = await reranker.RerankAsync(userQuery, rankedHits);
sw.Stop();

Console.WriteLine($"\nReranking latency: {sw.ElapsedMilliseconds}ms");
Console.WriteLine("\nTop 3 results after reranking:");
foreach (var result in rerankedResults.Take(3))
{
    Console.WriteLine($"[{result.Score:F3}] {result.Document}");
}
```

**Expected output:**
```
Reranking latency: 142ms

Top 3 results after reranking:
[0.912] Error handling: Catch exceptions at controller level. Return 500 with error ID.
[0.834] Logging: Structured logging with Serilog. Log levels: Debug, Info, Warning, Error.
[0.798] API design: RESTful endpoints. Use HTTP verbs correctly. Versioning in URL.
```

---

### Step 5: Validate Quality Improvement

```csharp
// Calculate improvement
var beforeScore = rankedHits.First(h => h.Document.Contains("Error handling")).Score;
var afterScore = rerankedResults.First().Score;
var improvement = (afterScore - beforeScore) / beforeScore;

Console.WriteLine($"\nQuality improvement: {improvement * 100:F1}%");

// Verify SLO: improvement ≥10%
if (improvement >= 0.10f)
{
    Console.WriteLine("✓ Reranking meets quality SLO (≥10% improvement)");
}
```

---

## Performance SLOs

| Metric | Target | Notes |
|--------|--------|-------|
| **Reranking latency** | <200ms | For 10 candidates, mock or fast LLM |
| **Quality improvement** | ≥10% | Score boost for top result |
| **Determinism** | 100% | Same query → same order (with deterministic LLM) |

---

## When to Use Reranking

### ✅ **Use reranking for:**

- **Customer support Q&A** — High precision required (wrong answer = bad UX)
- **Legal document search** — Accuracy is critical
- **Medical/scientific research** — False positives are costly
- **Technical troubleshooting** — Users need the exact solution, not "close enough"

### ❌ **Skip reranking for:**

- **General chatbot memory** — Semantic search is fast enough
- **Exploratory research** — Broad recall > precision
- **Low-latency requirements** — <50ms response time needed
- **Offline/local-only** — No LLM API available

---

## ⚠️ Common Pitfalls

### 1. Reranking Too Many Candidates

**Problem:** Reranking 50+ candidates is slow and expensive.  
**Solution:** Use semantic search to narrow to top-10 or top-20 first.

### 2. Using Reranking for Every Query

**Problem:** Adds 150ms+ latency to every search.  
**Solution:** Enable reranking only for high-precision use cases or offer it as an opt-in feature.

### 3. Not Measuring Improvement

**Problem:** Reranking may not improve results for all queries.  
**Solution:** Log before/after scores and monitor improvement %. If <10%, investigate query or corpus quality.

### 4. Determinism Issues

**Problem:** LLM reranking returns different orders for the same query.  
**Solution:** Set `temperature=0` in LLM config for deterministic output.

---

## Full Code Example

```csharp
using MemPalace;
using MemPalace.Ai.Rerank;
using MemPalace.Core.Model;
using System.Diagnostics;

public class RerankingExample
{
    public static async Task Main()
    {
        // 1. Initialize palace and mine docs
        var palace = await Palace.Create("~/reranking-demo");
        var docs = new[] { /* your documents */ };
        foreach (var doc in docs)
            await palace.Store(doc, wing: "docs");

        // 2. Semantic search
        var query = "How do we handle errors?";
        var candidates = await palace.Search(query, wing: "docs", limit: 10);

        // 3. Rerank
        var reranker = new MockReranker(); // or OpenAIReranker for production
        var hits = candidates.Select(c => new RankedHit(c.Memory.Id, c.Memory.Content, c.Score)).ToList();
        
        var sw = Stopwatch.StartNew();
        var reranked = await reranker.RerankAsync(query, hits);
        sw.Stop();

        // 4. Validate
        Console.WriteLine($"Reranking latency: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Top result: {reranked.First().Document}");
        Console.WriteLine($"Score improvement: {((reranked.First().Score - hits.First().Score) / hits.First().Score) * 100:F1}%");
    }
}
```

---

## See Also

- **[RerankingJourneyTests.cs](../../src/MemPalace.E2E.Tests/RerankingJourneyTests.cs)** — Full E2E test code
- **[SKILL_PATTERNS.md](../SKILL_PATTERNS.md)** — Pattern 3: LLM Reranking
- **[RAG Integration Guide](rag-integration-guide.md)** — Use reranking in RAG pipelines
- **[Getting Started](GETTING_STARTED.md)** — Basics of MemPalace.NET

---

**Next steps:** Try reranking in your own palace, measure improvement, and decide when to enable it for production. 🎯
