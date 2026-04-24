# MemPalace.NET v0.1.0 — Release Notes

**Release Date:** April 24, 2026

MemPalace.NET v0.1.0 is the first public release of the .NET port of [MemPalace](https://github.com/MemPalace/mempalace). This release delivers local-first AI memory with semantic search, temporal knowledge graphs, MCP server integration, and agent framework support.

## 🎯 Highlights

- **Local-first by default** — ONNX embeddings via ElBruno.LocalEmbeddings (no API keys, no cloud calls)
- **Microsoft.Extensions.AI** — swap embedders and LLMs with zero lock-in
- **Microsoft Agent Framework** — each agent gets its own memory diary
- **MCP server** — expose your palace as Model Context Protocol tools
- **Temporal knowledge graph** — track entity relationships with validity windows
- **SQLite backend** — pure managed storage, clear upgrade path to vector stores

## 📦 What's Included

### Core Libraries
- **MemPalace.Core** — domain types, storage interfaces, PalaceRef value object
- **MemPalace.Backends.Sqlite** — SQLite backend with BLOB vectors + cosine similarity
- **MemPalace.Ai** — M.E.AI integration (ONNX default, Ollama/OpenAI optional)
- **MemPalace.Mining** — filesystem + conversation transcript miners
- **MemPalace.Search** — semantic, keyword, hybrid search + optional reranking
- **MemPalace.KnowledgeGraph** — temporal triples with validity windows
- **MemPalace.Mcp** — Model Context Protocol server (29 tools)
- **MemPalace.Agents** — Agent Framework integration + per-agent diaries

### Tools
- **mempalacenet** — CLI tool (`dotnet tool install -g mempalacenet`)
- **mempalacenet-bench** — Benchmark harness for LongMemEval/LoCoMo/ConvoMem

### Test Coverage
- **129 tests** — all green, covering conformance suite, search quality, KG operations, agent integration

## 🚀 Getting Started

```bash
# Install CLI tool
dotnet tool install -g mempalacenet --version 0.1.0-preview.1

# Initialize a palace
mempalacenet init ~/my-palace

# Mine content
mempalacenet mine ~/my-code --wing work --mode files

# Search
mempalacenet search "authentication patterns" --hybrid --rerank

# Start MCP server
mempalacenet mcp --palace ~/my-palace
```

## 📚 Documentation

Full documentation at [`docs/`](https://github.com/elbruno/mempalacenet/tree/main/docs):
- [Architecture](https://github.com/elbruno/mempalacenet/blob/main/docs/architecture.md)
- [CLI Reference](https://github.com/elbruno/mempalacenet/blob/main/docs/cli.md)
- [AI Integration](https://github.com/elbruno/mempalacenet/blob/main/docs/ai.md)
- [MCP Server](https://github.com/elbruno/mempalacenet/blob/main/docs/mcp.md)
- [Agents](https://github.com/elbruno/mempalacenet/blob/main/docs/agents.md)
- [Benchmarks](https://github.com/elbruno/mempalacenet/blob/main/docs/benchmarks.md)

## ⚠️ Known Limitations

- **Vector storage:** SQLite backend uses O(n) brute-force cosine similarity. This is acceptable for <100K vectors but will be slow beyond that. Upgrade to [sqlite-vec](https://github.com/asg017/sqlite-vec) or Qdrant is planned post-v0.1.
- **Keyword search:** Currently uses simple token overlap. BM25 implementation planned post-v0.1.
- **Wake-up summaries:** Conversation context summaries (`mempalacenet wake-up`) not yet implemented.

## 🔗 Links

- **GitHub:** https://github.com/elbruno/mempalacenet
- **Original MemPalace:** https://github.com/MemPalace/mempalace
- **ElBruno.LocalEmbeddings:** https://github.com/elbruno/LocalEmbeddings
- **License:** [MIT](https://github.com/elbruno/mempalacenet/blob/main/LICENSE)

## 👤 Author

**Bruno Capuano** — [@elbruno](https://github.com/elbruno)

---

**Full Changelog:** https://github.com/elbruno/mempalacenet/blob/main/docs/CHANGELOG.md
