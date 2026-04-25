# 🧠 MemPalace.NET: Local-First AI Memory That Actually Remembers

**Your AI agent forgot the code you discussed 10 minutes ago. Again.**

Large Language Models are incredible at reasoning and generation, but they hallucinate facts, lose context across conversations, and can't effectively search through thousands of documents. The memory problem is real: your AI assistant that writes brilliant code can't remember what you told it yesterday, or worse, makes up details when it should admit ignorance.

**MemPalace.NET solves this.** It's a local-first semantic memory system for .NET that stores everything verbatim, searches semantically, and organizes knowledge through an intuitive hierarchy. No cloud calls by default. No vendor lock-in. Just pure .NET powered by ONNX embeddings, SQLite, and the Microsoft.Extensions.AI ecosystem.

And the .NET open-source community needed this — until now, Python had all the sophisticated memory solutions while .NET developers cobbled together fragile RAG pipelines or paid for cloud services.

---

## 🏰 What Is MemPalace.NET?

MemPalace.NET is a **structured semantic memory system** that gives AI agents a reliable long-term memory. Think of it as a personal knowledge base that:

- **Stores information verbatim** — No summaries, no compression, no hallucination risk
- **Searches semantically** — Find "authentication errors" even if you stored "login failures"  
- **Organizes hierarchically** — Wings (projects) → Rooms (topics) → Drawers (collections)
- **Runs locally** — ONNX embeddings via [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings), zero API calls
- **Integrates seamlessly** — Model Context Protocol (MCP) server for Claude Desktop, VS Code, and any MCP client

### The Memory Problem in Practice

Imagine you're building an AI customer support agent. A user asks: *"How do I reset my password?"*

Your agent needs to:
1. Search through 500+ support articles
2. Find the relevant password reset procedure
3. Remember this user's previous conversation about 2FA issues
4. Maintain context across multiple turns

**Traditional LLM approaches fail:**
- ❌ **Raw LLM:** Hallucinates password reset steps that don't exist
- ❌ **Naive RAG:** Returns irrelevant chunks, loses conversation context
- ❌ **Cloud vector DB:** Expensive, latency-sensitive, privacy concerns

**MemPalace.NET succeeds:**
- ✅ Semantic search finds the right article (stored verbatim)
- ✅ Conversation diary maintains user context across sessions
- ✅ Knowledge graph tracks relationships (user ↔ 2FA issue ↔ password reset)
- ✅ Everything runs locally with sub-100ms query times

---

## 📚 Architecture: How It Works

MemPalace.NET is built as a modular .NET solution with clear separation of concerns:

```
┌─────────────────────────────────────────────────┐
│  MemPalace.Cli / MemPalace.Mcp / Your App     │
├─────────────────────────────────────────────────┤
│  MemPalace.Agents (Microsoft Agent Framework)  │
│  MemPalace.Search (Semantic + Hybrid + Rerank) │
│  MemPalace.Mining (Content Ingestion)          │
│  MemPalace.KnowledgeGraph (Temporal Triples)   │
├─────────────────────────────────────────────────┤
│  MemPalace.Ai (M.E.AI Embedder Abstraction)    │
├─────────────────────────────────────────────────┤
│  MemPalace.Core (Domain Types + IBackend)      │
├─────────────────────────────────────────────────┤
│  MemPalace.Backends.Sqlite (Default Storage)   │
└─────────────────────────────────────────────────┘
```

**Key architectural decisions:**

1. **Local-first by default:** ONNX embeddings run in-process. No OpenAI, no Cohere, no cloud dependencies unless you explicitly opt in.

2. **Microsoft.Extensions.AI integration:** Swap embedders with zero friction. Start with ONNX, upgrade to Ollama or OpenAI when needed.

3. **Verbatim storage:** Unlike lossy summarization systems, MemPalace stores your original content. Embeddings are for search only — retrieval returns the exact text you stored.

4. **Hierarchical organization:** 
   - **Palace** = Your entire memory system
   - **Wing** = High-level category (e.g., "work", "research", "personal")
   - **Room** = Topic-level grouping (e.g., "authentication", "payments")
   - **Drawer** = Collection of related memories (implements ICollection interface)

5. **Temporal knowledge graph:** Track entity relationships with validity windows. Knows that "UserX had 2FA issues" from Jan 10-15, then resolved.

---

## 💻 Real-World Use Case: Customer Support Agent

Let's walk through a concrete example. You're building a support agent that needs to remember customer interactions and search internal documentation.

### Step 1: Initialize Your Palace

```bash
dotnet tool install -g mempalacenet --version 0.1.0-preview.1
mempalacenet init ~/support-palace
```

### Step 2: Mine Your Documentation

```bash
# Ingest all support articles
mempalacenet mine ~/docs/support-articles \
  --wing support \
  --room articles \
  --mode files

# Ingest previous customer conversations
mempalacenet mine ~/transcripts/january \
  --wing support \
  --room conversations \
  --mode convos
```

Behind the scenes, this:
- Recursively walks directories (respecting .gitignore)
- Chunks files intelligently (Markdown-aware, respects code blocks)
- Generates ONNX embeddings locally (no API calls)
- Stores verbatim content in SQLite with BLOB vectors

### Step 3: Semantic Search

```csharp
using MemPalace.Core;
using MemPalace.Ai;
using MemPalace.Search;

// Initialize palace with local ONNX embedder
var palace = new Palace("~/support-palace");
var embedder = new OnnxEmbedder(); // Default: all-MiniLM-L6-v2

var searcher = new SemanticSearchEngine(palace, embedder);

// User asks about password reset
var results = await searcher.SearchAsync(
    query: "How do I reset my password?",
    wing: "support",
    room: "articles",
    topK: 5
);

foreach (var result in results)
{
    Console.WriteLine($"[Score: {result.Score:F3}] {result.Document}");
    Console.WriteLine($"Source: {result.Metadata["source"]}\n");
}
```

**Output:**
```
[Score: 0.891] To reset your password, navigate to Settings > Security > Password...
Source: docs/password-reset-guide.md

[Score: 0.847] If you've enabled 2FA, you'll need your authenticator code...
Source: docs/2fa-troubleshooting.md

[Score: 0.823] Password requirements: minimum 12 characters, one uppercase...
Source: docs/security-policies.md
```

### Step 4: Maintain Conversation Context with Agent Diaries

```csharp
using MemPalace.Agents;
using Microsoft.Extensions.AI;

// Create an agent with its own memory diary
var agent = new MemPalaceAgent(
    name: "SupportBot",
    palace: palace,
    wing: "support",
    embedder: embedder
);

// Agent remembers across sessions
await agent.RecordInteraction(
    userId: "user123",
    message: "I can't log in, getting 2FA errors",
    response: "I see. Let me check our 2FA troubleshooting guide..."
);

// Later conversation (same user)
var context = await agent.GetUserContext("user123");
// Returns: Previous 2FA issues, relevant articles shown, resolution status
```

### Step 5: Expose via MCP for Claude Desktop

```bash
# Start MCP server
mempalacenet mcp --palace ~/support-palace --port 3000
```

Add to your `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "mempalace-support": {
      "command": "mempalacenet",
      "args": ["mcp", "--palace", "C:/Users/you/support-palace"]
    }
  }
}
```

Now Claude Desktop has 7 MCP tools:
- `mempalace_search` — Semantic search across wings/rooms
- `mempalace_add` — Store new memories
- `mempalace_get` — Retrieve by ID or filter
- `mempalace_update` — Modify existing memories
- `mempalace_kg_add` — Add temporal relationships
- `mempalace_kg_query` — Query knowledge graph
- `mempalace_health` — System diagnostics

---

## ✨ Feature Showcase

### 🔍 Semantic Search with Embeddings

Traditional keyword search fails when your query words don't match stored content. MemPalace.NET uses **semantic embeddings** to understand meaning:

```bash
# Finds results even if exact words don't match
mempalacenet search "authentication problems" --wing support
# Returns: "login failures", "access denied errors", "credential issues"
```

**How it works:**
- Content and queries are embedded into 384-dimensional vectors (default ONNX model)
- Cosine similarity finds nearest neighbors in embedding space
- SQLite BLOB storage + custom cosine function (upgradeable to sqlite-vec)

### 🕸️ Knowledge Graph for Relationships

Track entities and their temporal relationships:

```csharp
var kg = new KnowledgeGraph(palace);

// Store relationships with validity windows
await kg.AddTripleAsync(
    subject: "user123",
    predicate: "has_issue",
    object: "2fa_error",
    validFrom: DateTime.UtcNow.AddDays(-5),
    validTo: DateTime.UtcNow  // Issue resolved today
);

// Query: "Who had authentication issues last week?"
var results = await kg.QueryPattern(
    subjectFilter: null,  // Any user
    predicateFilter: "has_issue",
    objectFilter: "2fa_error",
    timeRange: (lastWeek, today)
);
```

Use cases:
- Customer issue tracking
- Document versioning and updates
- Team knowledge evolution over time

### 🔌 MCP Integration: AI-Native Interface

The Model Context Protocol (MCP) lets any AI client use your palace as a tool:

```typescript
// Claude Desktop automatically gets these tools:
const tools = [
  "mempalace_search",    // Semantic search
  "mempalace_add",       // Store memories
  "mempalace_get",       // Retrieve by filter
  "mempalace_kg_query",  // Graph traversal
  // ... 7 tools total
];
```

**Works with:**
- Claude Desktop (Anthropic)
- VS Code with MCP extensions
- Any custom MCP client

### 🏠 Local-First, Privacy-Respecting Design

**No cloud calls by default:**
- ONNX embeddings run in-process (via [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings))
- SQLite storage on your local disk
- No telemetry, no tracking, no external dependencies

**But flexible when you need it:**
```csharp
// Swap to OpenAI embeddings with one line
var embedder = new OpenAIEmbedder("text-embedding-3-small", apiKey);
```

Microsoft.Extensions.AI makes embedder swapping trivial. Start local, upgrade to cloud only if needed.

### 🛠️ .NET 10 Modern Tooling

Built on the latest .NET stack:
- **C# 13** language features
- **Minimal APIs** for MCP server
- **IAsyncEnumerable** for streaming large result sets
- **System.Text.Json** for serialization (zero Newtonsoft dependencies)
- **Spectre.Console** for beautiful CLI output
- **xUnit + FluentAssertions** for 152 passing tests

---

## 🚀 Getting Started (5 Minutes)

### 1. Install the CLI

```bash
dotnet tool install -g mempalacenet --version 0.1.0-preview.1
```

### 2. Initialize a Palace

```bash
mempalacenet init ~/my-palace
```

### 3. Mine Some Content

```bash
# Mine your project documentation
mempalacenet mine ~/my-project/docs --wing work --mode files

# Mine conversation logs
mempalacenet mine ~/my-convos --wing personal --mode convos
```

### 4. Search Semantically

```bash
mempalacenet search "how to handle authentication errors" --hybrid --rerank
```

### 5. Integrate with Your App

**NuGet packages available:**
```bash
dotnet add package MemPalace.Core --version 0.1.0-preview.1
dotnet add package MemPalace.Ai --version 0.1.0-preview.1
```

**Sample code:**
```csharp
using MemPalace.Core;
using MemPalace.Ai;

var palace = new Palace("~/my-palace");
var embedder = new OnnxEmbedder();
var backend = new SqliteBackend();

var collection = await backend.GetCollectionAsync(
    palace.Ref,
    name: "my-docs",
    create: true,
    embedder: embedder
);

await collection.AddAsync(new[] {
    new EmbeddedRecord(
        id: "doc1",
        document: "Your content here",
        metadata: new() { ["source"] = "README.md" }
    )
});

var results = await collection.QueryAsync(
    queryEmbeddings: await embedder.EmbedAsync(["search query"]),
    nResults: 10
);
```

**Explore examples:**
- [`examples/basic-usage/`](../examples/basic-usage/) — Palace initialization, mining, search
- [`examples/agent-diary/`](../examples/agent-diary/) — Per-agent conversation tracking
- [`examples/mcp-client/`](../examples/mcp-client/) — MCP tool integration

**CLI quick reference:**
- [`docs/cli.md`](cli.md) — Complete command reference
- [`docs/concepts.md`](PLAN.md) — Wings, rooms, drawers explained
- [`docs/architecture.md`](architecture.md) — Solution structure

---

## 🎯 Why This Matters

AI agents are only as good as their memory. Without reliable, searchable storage, they're just expensive autocomplete.

**MemPalace.NET gives .NET developers:**
1. **Production-ready memory** — No more duct-taping ChromaDB into your .NET app
2. **Local-first by default** — Privacy, cost control, and offline capability
3. **Microsoft.Extensions.AI integration** — Swap embedders without rewriting code
4. **Agent Framework support** — Per-agent diaries and context management
5. **MCP compatibility** — Works with Claude Desktop, VS Code, and future AI tools

This is the semantic memory system .NET developers deserved from day one.

---

## 🙌 Join the Community

**MemPalace.NET is open-source and community-driven.**

### ⭐ Star the repo
[github.com/elbruno/mempalacenet](https://github.com/elbruno/mempalacenet) — Show your support and stay updated

### 🚀 Try the examples
Clone the repo and explore [`examples/`](../examples/) to see MemPalace.NET in action

### 🐛 Report issues / 💡 Request features
Found a bug? Have an idea? Open an [issue](https://github.com/elbruno/mempalacenet/issues) or start a [discussion](https://github.com/elbruno/mempalacenet/discussions)

### 🤝 Contribute
We welcome contributions! Check out:
- [Contributing Guidelines](../.github/CONTRIBUTING.md)
- [Code of Conduct](../.github/CODE_OF_CONDUCT.md)
- [Good First Issues](https://github.com/elbruno/mempalacenet/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)

### 📣 Spread the word
- Share on Twitter/X, LinkedIn, Mastodon
- Write about your use case
- Submit to DEV.to, HackerNews, Reddit

**Built something cool with MemPalace.NET?** We'd love to feature your project in our [Community Showcase](https://github.com/elbruno/mempalacenet/discussions/categories/show-and-tell).

---

## 🗺️ Roadmap

**v0.1.0** (current) ships core memory operations, search, MCP server, and agents.

**Post-v0.1 priorities:**
- 🔄 Upgrade to [sqlite-vec](https://github.com/asg017/sqlite-vec) or Qdrant for >100K vectors
- 🔎 BM25 keyword search (currently token overlap)
- 📊 LongMemEval R@5 parity validation (target ≥ 91%)
- 💬 Conversation context summaries (`mempalace wake-up`)
- 🧩 Additional MCP tools for advanced workflows

**Have ideas for the roadmap?** Join the [discussion](https://github.com/elbruno/mempalacenet/discussions) and help shape the future.

---

## 💬 Final Thoughts

Large Language Models changed how we write code, but their memory problem remains unsolved. MemPalace.NET gives your AI agents the long-term memory they need to be truly useful.

**Local-first. Privacy-respecting. Built for .NET.**

Try it today. Your AI agents will thank you (and actually remember that they did).

```bash
dotnet tool install -g mempalacenet --version 0.1.0-preview.1
mempalacenet init ~/my-palace
mempalacenet mine . --wing demo --mode files
mempalacenet search "your first query"
```

🏰 **Welcome to your Memory Palace.**

---

**Author:** [Bruno Capuano](https://github.com/elbruno)  
**License:** [MIT](../LICENSE)  
**Project:** [github.com/elbruno/mempalacenet](https://github.com/elbruno/mempalacenet)  
**Credits:** Inspired by the original [MemPalace](https://github.com/MemPalace/mempalace) (Python)
