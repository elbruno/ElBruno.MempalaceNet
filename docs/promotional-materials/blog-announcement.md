# MemPalace.NET v0.5.0: Semantic Memory for Local AI Agents

**Published:** April 25, 2026  
**Author:** Bruno Capuano  
**Category:** Release Announcement

---

## TL;DR

MemPalace.NET v0.5.0 is now available on NuGet! It's a local-first semantic memory system for .NET AI agents that stores everything verbatim, searches semantically, and organizes knowledge through a hierarchical structure—all without requiring cloud API calls.

🔗 **Install:** `dotnet tool install -g mempalacenet --version 0.5.0`  
📦 **NuGet:** [mempalacenet](https://www.nuget.org/packages/mempalacenet)  
⭐ **GitHub:** [elbruno/mempalacenet](https://github.com/elbruno/mempalacenet)

---

## What's New in v0.5.0

This release marks a significant milestone in bringing production-ready semantic memory to .NET AI agents. Here's what's included:

### Core Features

**Local-First Architecture**  
No cloud dependencies by default. MemPalace.NET uses ONNX embeddings via [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings), meaning your data stays on your machine and you don't need API keys to get started.

**Microsoft.Extensions.AI Integration**  
Built on Microsoft's AI abstraction layer, you can swap embedders (ONNX, Ollama, OpenAI) with zero lock-in. The same clean interface works across all providers.

**Microsoft Agent Framework Support**  
Each agent gets its own memory diary, making it perfect for multi-agent systems where you need isolated, searchable memory per agent.

**Model Context Protocol (MCP) Server**  
Expose your palace as 29 MCP tools for use in Claude Desktop, VS Code, and other MCP-compatible clients. Read, write, search, and manage knowledge graphs from your favorite AI tools.

**Temporal Knowledge Graph**  
Track entity relationships with validity windows. Know when facts were true, when they changed, and maintain a complete timeline of your knowledge.

**Hybrid Search with Reranking**  
Combine semantic search (vector similarity) with keyword matching using Reciprocal Rank Fusion (RRF). Optionally rerank results with an LLM for maximum relevance.

---

## Why Local Semantic Memory Matters

AI agents are only as good as their memory. Without persistent, searchable memory, agents can't:

- **Learn from past interactions** — every conversation starts from zero
- **Build context over time** — no way to connect today's work to last week's insights
- **Scale beyond token limits** — LLM context windows are finite
- **Maintain consistent knowledge** — facts get lost or contradicted

MemPalace.NET solves this by giving your agents **semantic memory** that:

1. **Stores everything verbatim** — no summarization, no data loss
2. **Searches semantically** — find concepts, not just keywords
3. **Organizes hierarchically** — wings → rooms → drawers structure mirrors how you think
4. **Runs 100% local** — your data never leaves your machine (unless you want it to)
5. **Integrates seamlessly** — drop it into existing Microsoft.Extensions.AI projects

This is especially powerful for:

- **Research agents** that accumulate knowledge over weeks or months
- **Personal assistants** that remember your preferences and past conversations
- **Development tools** that learn your codebase and coding patterns
- **Knowledge workers** building a second brain for their domain

---

## Getting Started: First 5 Minutes

### Installation

```bash
# Install the CLI tool globally
dotnet tool install -g mempalacenet --version 0.5.0
```

### Initialize Your Palace

```bash
# Create a new palace in your home directory
mempalacenet init ~/my-palace
```

This creates a SQLite database with the wings/rooms/drawers structure.

### Mine Your First Content

```bash
# Index your code projects
mempalacenet mine ~/my-code --wing work --mode files

# Index conversation transcripts
mempalacenet mine ~/my-convos --wing personal --mode convos
```

The miner respects `.gitignore` files and chunks content intelligently (2000 chars with 200-char overlap).

### Search Semantically

```bash
# Basic semantic search
mempalacenet search "how do I handle authentication errors?"

# Hybrid search with LLM reranking
mempalacenet search "latest React patterns" --hybrid --rerank
```

### Use in Code

```csharp
using MemPalace.Core;
using MemPalace.Backends.Sqlite;
using MemPalace.Ai;

// Initialize backend
var backend = new SqliteBackend("palace.db");
await backend.InitializeAsync();

// Get or create a collection
var collection = await backend.GetCollectionAsync(
    new PalaceRef("work", "projects", "myapp")
);

// Create embedder (local ONNX by default)
var embedder = new OnnxEmbedder();

// Store a memory
await collection.AddAsync(new Memory
{
    Id = "mem-001",
    Content = "User authentication uses JWT tokens with 15-minute expiry.",
    Metadata = new Dictionary<string, object>
    {
        ["source"] = "auth-service",
        ["timestamp"] = DateTime.UtcNow
    }
});

// Semantic search
var results = await collection.QueryAsync(
    queryEmbedding: await embedder.EmbedAsync("JWT token expiry"),
    limit: 5
);

foreach (var result in results)
{
    Console.WriteLine($"[{result.Score:F3}] {result.Memory.Content}");
}
```

That's it! You now have semantic memory for your AI agents.

---

## Architecture Highlights

MemPalace.NET is designed as a modular .NET solution with clean separation of concerns:

**MemPalace.Core**  
Domain types, storage interfaces, and the `PalaceRef` value object for hierarchical addressing (wing/room/drawer).

**MemPalace.Backends.Sqlite**  
Default backend using SQLite with BLOB vectors and brute-force cosine similarity. Acceptable for <100K vectors, with a clear upgrade path to `sqlite-vec` or Qdrant.

**MemPalace.Ai**  
Microsoft.Extensions.AI integration with ONNX (default), Ollama, and OpenAI support. Tracks embedder identity to prevent mixing incompatible embeddings.

**MemPalace.Mining**  
Content ingestion pipeline with filesystem miner (respects `.gitignore`) and conversation transcript miner (JSONL/Markdown).

**MemPalace.Search**  
Semantic, keyword, and hybrid search strategies with optional LLM reranking. Uses RRF fusion (k=60) for hybrid mode.

**MemPalace.KnowledgeGraph**  
Temporal entity-relationship graph with validity windows. Pattern queries support wildcards for flexible matching.

**MemPalace.Mcp**  
Model Context Protocol server exposing 29 tools for palace operations, knowledge graph management, and agent diary access.

**MemPalace.Agents**  
Microsoft Agent Framework integration with per-agent wings and memory diaries. Agents discover each other via `mempalace_list_agents`.

**MemPalace.Cli**  
Spectre.Console-based CLI with rich output and comprehensive command coverage.

**MemPalace.Benchmarks**  
LongMemEval, LoCoMo, and ConvoMem benchmark harnesses with R@5 metric validation (target ≥ 91%).

---

## Next Steps & Roadmap

MemPalace.NET v0.5.0 is production-ready for local development and experimentation. Here's what's coming next:

### Post-v0.5.0 Enhancements

**Vector Store Upgrade**  
Migrate to [sqlite-vec](https://github.com/asg017/sqlite-vec) or Qdrant for >100K vectors. Current SQLite backend uses O(n) brute-force cosine similarity, which is fine for smaller datasets but doesn't scale beyond 100K items.

**BM25 Keyword Search**  
Replace token overlap with proper BM25 scoring for better keyword matching in hybrid search mode.

**Conversation Context Summaries**  
Implement `mempalace wake-up` command to generate summaries of recent context for quick agent briefings.

**LongMemEval R@5 Parity**  
Complete validation against standard benchmarks with target ≥ 91% recall at 5.

**Multi-Backend Support**  
First-class support for Qdrant, Milvus, and other vector stores via pluggable backends.

---

## Get Involved

MemPalace.NET is open source (MIT license) and we welcome contributions!

**Install from NuGet:**
```bash
dotnet tool install -g mempalacenet --version 0.5.0
```

**Star on GitHub:**  
⭐ [github.com/elbruno/mempalacenet](https://github.com/elbruno/mempalacenet)

**Contribute:**
- 📝 [Contributing Guidelines](https://github.com/elbruno/mempalacenet/blob/main/.github/CONTRIBUTING.md)
- 🐛 [Report Issues](https://github.com/elbruno/mempalacenet/issues)
- 💬 [Discussions](https://github.com/elbruno/mempalacenet/discussions)

**Questions?**  
Open a [discussion](https://github.com/elbruno/mempalacenet/discussions) or reach out to [@elbruno](https://github.com/elbruno).

---

## Credits

MemPalace.NET is a .NET port of the original [MemPalace](https://github.com/MemPalace/mempalace) project (Python), rebuilt with Microsoft.Extensions.AI and Microsoft Agent Framework integration.

**Author:** [Bruno Capuano](https://github.com/elbruno)  
**Default Embedder:** [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings) (ONNX)

---

**Ready to give your AI agents a memory?** Install MemPalace.NET today and join the local-first AI movement! 🚀

```bash
dotnet tool install -g mempalacenet --version 0.5.0
mempalacenet init ~/my-palace
mempalacenet mine ~/my-code --wing work --mode files
mempalacenet search "your first semantic query"
```

---

_MemPalace.NET is open source software licensed under MIT. All contributions are welcome._
