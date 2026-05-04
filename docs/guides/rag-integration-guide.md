# RAG Integration Guide

**Build production-ready Retrieval-Augmented Generation pipelines** — Transform document corpora into intelligent Q&A systems with MemPalace.NET's high-precision semantic search.

**Read time:** ~8 minutes | **Difficulty:** Intermediate to Advanced

---

## What is RAG?

**Retrieval-Augmented Generation (RAG)** is a pattern where an LLM's responses are grounded in retrieved documents:

1. **User asks a question** → "How do we handle distributed caching?"
2. **System retrieves relevant documents** → Semantic search finds top-5 matches
3. **System injects context into prompt** → Docs → LLM prompt
4. **LLM generates response** → Answer grounded in retrieved context

**Why RAG?**  
- **Accuracy:** LLM cites specific facts from your docs (not hallucinations)
- **Freshness:** Update docs, not model weights
- **Explainability:** You know which docs the LLM used
- **Cost:** No fine-tuning required

---

## Why MemPalace.NET is Ideal for RAG

| Feature | Benefit for RAG |
|---------|----------------|
| **R@5 ≥96.6%** | High recall ensures relevant docs are found |
| **Local ONNX embeddings** | No API costs for embedding queries/docs |
| **Hybrid search** | Combines semantic + keyword for edge cases |
| **Fast retrieval** | <50ms search latency (excluding LLM) |
| **Verbatim storage** | No summarization = no information loss |

---

## The RAG Pipeline (4 Steps)

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐      ┌─────────────┐
│ 1. Mine     │ ───> │ 2. Search    │ ───> │ 3. Inject   │ ───> │ 4. Respond  │
│ Documents   │      │ Semantically │      │ Context     │      │ (LLM)       │
└─────────────┘      └──────────────┘      └─────────────┘      └─────────────┘
   Offline              ~50ms                 ~10ms                ~1-3s
```

### Phase 1: Mine (Offline, One-Time)
- Load documents into palace
- Generate embeddings
- Store in SQLite backend

### Phase 2: Search (Runtime, Fast)
- User query → embedding
- Semantic search → top-5 docs

### Phase 3: Inject (Runtime, Fast)
- Format retrieved docs
- Insert into LLM prompt

### Phase 4: Respond (Runtime, LLM-Dependent)
- LLM reads context
- Generates grounded response

---

## When to Use RAG

### ✅ **Use RAG for:**

- **Documentation Q&A** — User asks questions about your product docs
- **Knowledge base search** — Internal wiki, support articles
- **Research synthesis** — Summarize findings from 50+ papers
- **Code search** — "Find examples of async/await in our codebase"
- **Customer support** — Ground agent responses in help articles

### ❌ **Skip RAG for:**

- **General chatbots** — If no specific corpus is needed
- **Real-time data** — RAG is for static/semi-static corpora
- **Creative writing** — LLM imagination > retrieval

---

## Step-by-Step Walkthrough

### Prerequisites

- MemPalace.NET installed (`dotnet add package mempalacenet`)
- An `IChatClient` registered (OpenAI, Azure OpenAI, Ollama, etc.)
- A document corpus (50+ docs recommended for realistic testing)

---

### Step 1: Mine Corpus (Offline)

**Recommended corpus size:** 50+ documents for production, 10–20 for testing.

```csharp
using MemPalace;
using MemPalace.Core.Model;

// Initialize palace
var palace = await Palace.Create("~/rag-demo");

// Create a realistic document corpus (caching examples)
var documents = new[]
{
    "Distributed caching with Redis provides in-memory storage for session data and frequently accessed content. TTL is typically 1 hour with sliding expiration.",
    "Redis cluster mode enables horizontal scaling and automatic failover for high availability distributed caching.",
    "Cache invalidation strategies include TTL-based expiration, event-driven purging, and manual cache clearing on data updates.",
    "Database connection pooling reduces latency by reusing existing connections. Max 20 connections per instance.",
    "PostgreSQL query optimization using EXPLAIN ANALYZE to identify slow queries and missing indexes.",
    "Authentication middleware validates JWT tokens and enforces role-based access control (RBAC).",
    "API rate limiting prevents abuse by restricting requests per client IP to 100/minute.",
    "Logging infrastructure uses Serilog for structured logging with JSON output to Application Insights.",
    "Message queue processing with RabbitMQ for asynchronous task execution and event-driven workflows.",
    "Monitoring setup with Prometheus metrics and Grafana dashboards for real-time observability.",
    "Cache warming preloads frequently accessed data on application startup to reduce cold start latency.",
    "Redis persistence options: RDB snapshots for point-in-time backups, AOF for write durability.",
    "Frontend build pipeline uses Webpack with tree shaking and code splitting for optimal bundle size.",
    "Docker multi-stage builds reduce image size by excluding build dependencies from runtime container.",
    "CI/CD pipeline stages: build, test, security scan, deploy to staging, smoke test, deploy to production."
};

// Store in palace (embeddings generated automatically)
foreach (var doc in documents)
{
    await palace.Store(doc, wing: "caching-docs", metadata: new Dictionary<string, object?>
    {
        { "category", "distributed-systems" },
        { "indexed_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
    });
}

Console.WriteLine($"✓ Mined {documents.Length} documents into palace");
```

**Tip:** For file-based corpora, use the CLI:

```bash
mempalacenet mine ~/docs --wing documentation --mode files
```

---

### Step 2: Create Query Handler (Semantic Search)

```csharp
using System.Diagnostics;

public class RAGQueryHandler
{
    private readonly Palace _palace;
    private readonly IChatClient _llm;

    public RAGQueryHandler(Palace palace, IChatClient llm)
    {
        _palace = palace;
        _llm = llm;
    }

    public async Task<string> AnswerQuestion(string userQuery)
    {
        // Phase 2: Search
        var sw = Stopwatch.StartNew();
        var searchResults = await _palace.Search(
            query: userQuery,
            wing: "caching-docs",
            limit: 5
        );
        sw.Stop();

        Console.WriteLine($"Search: {sw.ElapsedMilliseconds}ms, {searchResults.Count} docs retrieved");

        if (searchResults.Count == 0)
        {
            return "I couldn't find relevant information in the knowledge base.";
        }

        // Phase 3: Inject context
        var context = string.Join("\n\n", searchResults.Select((r, i) => 
            $"{i + 1}. {r.Memory.Content}"));

        var prompt = $@"You are a helpful technical assistant. Answer the question based on the provided context.

Context:
{context}

Question: {userQuery}

Answer based on the context above:";

        // Phase 4: Generate response
        sw.Restart();
        var response = await _llm.CompleteAsync(prompt);
        sw.Stop();

        Console.WriteLine($"LLM: {sw.ElapsedMilliseconds}ms");

        return response.Message.Text;
    }
}
```

---

### Step 3: Execute RAG Pipeline

```csharp
// Usage example
var handler = new RAGQueryHandler(palace, chatClient);

var userQuery = "How do we handle distributed caching?";
var answer = await handler.AnswerQuestion(userQuery);

Console.WriteLine($"\nUser: {userQuery}");
Console.WriteLine($"Assistant: {answer}");
```

**Expected output:**

```
Search: 42ms, 5 docs retrieved
LLM: 1834ms

User: How do we handle distributed caching?
Assistant: Based on the provided context, distributed caching is handled using Redis, which provides in-memory storage for session data and frequently accessed content. Key configurations include:

1. **Redis cluster mode** for high availability with automatic failover and horizontal scaling
2. **TTL-based expiration** (typically 1 hour) with sliding expiration for frequently accessed items
3. **Cache invalidation strategies**: TTL-based, event-driven purging, or manual clearing on data updates
4. **Cache warming** to preload frequently accessed data on startup and reduce cold start latency
5. **Persistence options**: RDB snapshots for backups and AOF for durability

This approach ensures both performance and reliability for distributed caching scenarios.
```

---

### Step 4: Validate Quality Metrics

```csharp
public async Task ValidateRAGQuality(string query, string[] expectedKeywords)
{
    // 1. Search baseline: R@5 check
    var results = await _palace.Search(query, wing: "caching-docs", limit: 5);
    var hasRelevantDoc = results.Any(r => 
        expectedKeywords.Any(kw => r.Memory.Content.Contains(kw, StringComparison.OrdinalIgnoreCase)));

    Console.WriteLine($"R@5 check: {(hasRelevantDoc ? "✓ PASS" : "✗ FAIL")}");

    // 2. Context injection: verify all docs in prompt
    var context = string.Join("\n", results.Select(r => r.Memory.Content));
    var allDocsInjected = results.All(r => context.Contains(r.Memory.Content));

    Console.WriteLine($"Context injection: {(allDocsInjected ? "✓ PASS" : "✗ FAIL")}");

    // 3. Response quality: LLM cites specific details
    var response = await AnswerQuestion(query);
    var citesDetails = expectedKeywords.Any(kw => 
        response.Contains(kw, StringComparison.OrdinalIgnoreCase));

    Console.WriteLine($"Response quality: {(citesDetails ? "✓ PASS" : "✗ FAIL")}");
}

// Usage
await ValidateRAGQuality(
    query: "How do we handle distributed caching?",
    expectedKeywords: new[] { "Redis", "cluster", "TTL", "cache" }
);
```

---

## Full Code Example (50 Lines)

```csharp
using MemPalace;
using Microsoft.Extensions.AI;
using System.Diagnostics;

public class RAGPipeline
{
    private readonly Palace _palace;
    private readonly IChatClient _llm;

    public RAGPipeline(Palace palace, IChatClient llm)
    {
        _palace = palace;
        _llm = llm;
    }

    public async Task<(string answer, TimeSpan searchTime, TimeSpan llmTime)> Query(string question)
    {
        // Step 1: Search
        var sw = Stopwatch.StartNew();
        var docs = await _palace.Search(question, wing: "docs", limit: 5);
        var searchTime = sw.Elapsed;

        // Step 2: Inject
        var context = string.Join("\n\n", docs.Select((d, i) => $"{i + 1}. {d.Memory.Content}"));
        var prompt = $"Context:\n{context}\n\nQuestion: {question}\n\nAnswer:";

        // Step 3: Generate
        sw.Restart();
        var response = await _llm.CompleteAsync(prompt);
        var llmTime = sw.Elapsed;

        return (response.Message.Text, searchTime, llmTime);
    }

    public static async Task Main()
    {
        var palace = await Palace.Create("~/rag-demo");
        var llm = new OpenAIChatClient("gpt-4", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        var rag = new RAGPipeline(palace, llm);

        // Mine docs (one-time)
        var docs = new[] { "Redis provides in-memory caching...", /* more docs */ };
        foreach (var doc in docs)
            await palace.Store(doc, wing: "docs");

        // Query
        var (answer, searchTime, llmTime) = await rag.Query("How do we handle caching?");
        Console.WriteLine($"Search: {searchTime.TotalMilliseconds}ms");
        Console.WriteLine($"LLM: {llmTime.TotalMilliseconds}ms");
        Console.WriteLine($"Answer: {answer}");
    }
}
```

---

## Quality Metrics

### Search Baseline

| Metric | Target | Notes |
|--------|--------|-------|
| **Recall@5** | ≥96.6% | Phase 3 baseline (maintained) |
| **Search latency** | <50ms | Embedding + cosine similarity |
| **Precision@1** | ≥85% | Top result is relevant |

### Context Injection

| Check | Expected | Notes |
|-------|----------|-------|
| **All docs present** | 100% | Every retrieved doc in prompt |
| **No truncation** | ✓ | Full docs injected (no clipping) |
| **Correct format** | ✓ | LLM can parse context |

### Response Quality

| Metric | Target | Notes |
|--------|--------|-------|
| **Cites details** | ≥80% | Response mentions specific facts from context |
| **Not generic** | ✓ | Avoids "in general" / "typically" phrasing |
| **Coherent** | ✓ | Answer is logically structured |

### End-to-End Latency

| Component | Target | Notes |
|-----------|--------|-------|
| **Search** | <50ms | Embedding + query |
| **Injection** | <10ms | String formatting |
| **LLM** | 1–3s | Depends on model (GPT-4: ~2s) |
| **Total** | <500ms | Excluding LLM (search+inject only) |

---

## Optimization Tips

### 1. Hybrid Search for Edge Cases

```csharp
// Semantic search may miss exact keyword matches
var results = await palace.Search(query, wing: "docs", limit: 10, mode: SearchMode.Hybrid);
```

**When to use:**
- User query contains technical terms ("OAuth2", "PostgreSQL")
- Semantic search misses obvious keyword matches

### 2. LLM Reranking for Top-10 Refinement

```csharp
// Retrieve 10 candidates, rerank to 5
var candidates = await palace.Search(query, wing: "docs", limit: 10);
var reranked = await reranker.RerankAsync(query, candidates);
var topDocs = reranked.Take(5);
```

**Benefit:** +5-10% precision for high-stakes queries (see [Reranking Guide](reranking-workflow.md))

### 3. Caching Embeddings for Repeated Queries

```csharp
// Cache query embeddings (if same query repeated)
var cache = new Dictionary<string, float[]>();

if (!cache.TryGetValue(query, out var embedding))
{
    embedding = await palace.Embedder.EmbedAsync(new[] { query });
    cache[query] = embedding[0].ToArray();
}
```

**Benefit:** Saves 20–30ms per repeated query

### 4. Batch Processing for Large Corpora

```csharp
// Mine 1000+ docs in batches
var batches = documents.Chunk(50);
foreach (var batch in batches)
{
    var tasks = batch.Select(doc => palace.Store(doc, wing: "docs"));
    await Task.WhenAll(tasks);
}
```

**Benefit:** 3-5x faster than sequential mining

---

## ⚠️ Common Pitfalls

### 1. Mining Too Few Documents

**Problem:** RAG with 5 docs → poor recall.  
**Solution:** Mine ≥50 documents for realistic testing, 500+ for production.

### 2. Not Validating R@5

**Problem:** Search returns irrelevant docs → LLM hallucinates.  
**Solution:** Measure R@5 before deploying. If <80%, improve embeddings or add hybrid search.

### 3. Injecting Too Much Context

**Problem:** 20 docs → 10,000 tokens → slow LLM, high cost.  
**Solution:** Limit to 3–5 docs. Use reranking to refine top results.

### 4. Generic LLM Responses

**Problem:** LLM ignores context, gives generic answer.  
**Solution:** Strengthen system prompt:

```csharp
var prompt = $@"You MUST answer ONLY based on the context below. Do not use general knowledge.

Context:
{context}

Question: {query}

Answer (cite specific details from context):";
```

### 5. No Latency Monitoring

**Problem:** RAG is slow in production, users complain.  
**Solution:** Log latency for each phase:

```csharp
Console.WriteLine($"Search: {searchMs}ms, Inject: {injectMs}ms, LLM: {llmMs}ms, Total: {totalMs}ms");
```

---

## See Also

- **[RAGPipelineTests.cs](../../src/MemPalace.E2E.Tests/RAGPipelineTests.cs)** — Full E2E test code
- **[SKILL_PATTERNS.md](../SKILL_PATTERNS.md)** — Pattern 2: RAG Context Injection
- **[Reranking Workflow Guide](reranking-workflow.md)** — Improve RAG with reranking
- **[Agent Memory Diary Guide](agent-memory-diary.md)** — Use RAG for agent memory
- **[Getting Started](GETTING_STARTED.md)** — MemPalace.NET basics

---

**Next steps:** Build your RAG pipeline, measure R@5, and deploy to production. 🚀📚
