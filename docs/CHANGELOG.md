# Changelog

All notable changes to MemPalace.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-04-24

### Added

**Phase 0 — Scaffold & CI**
- Solution scaffold with 8+ projects targeting .NET 10
- GitHub Actions CI workflow with build + test + pack jobs
- MIT license and project documentation structure

**Phase 1 — Core Domain**
- `IBackend`, `ICollection`, `IEmbedder` contracts
- Immutable records for `Memory`, `QueryResult`, `GetResult`
- PalaceRef value object for hierarchical addressing
- Backend conformance test suite (xUnit)

**Phase 2 — SQLite Backend**
- Pure managed BLOB storage + brute-force cosine similarity
- Embedder identity guards (prevent mixing incompatible embeddings)
- Collection schema with migration helpers
- Conformance suite validated against SQLite backend

**Phase 3 — AI Integration**
- Microsoft.Extensions.AI abstractions for embedders
- Default: ElBruno.LocalEmbeddings (ONNX, no external runtime)
- Optional: Ollama, OpenAI providers (opt-in)
- Embedder identity tracking (models, dimensions)

**Phase 4 — Mining & Search**
- FileSystemMiner with .gitignore respect + 2000-char chunking
- ConversationMiner (JSONL + Markdown transcript support)
- Keyed DI services for pluggable miners ("filesystem", "conversation")
- Semantic search (vector cosine similarity)
- Hybrid search (RRF fusion with k=60, token overlap for keywords)
- Optional LLM reranking (opt-in via `SearchOptions.Rerank`)

**Phase 5 — CLI**
- Spectre.Console.Cli framework with rich output
- Commands: `init`, `mine`, `search`, `agents`, `kg`, `mcp`
- TypeRegistrar/TypeResolver DI integration
- Configuration via JSON + environment variables + user secrets

**Phase 6 — Knowledge Graph**
- Temporal entity-relationship graph (SQLite-backed)
- Triples with ValidFrom/ValidTo + RecordedAt timestamps
- EntityRef format: "type:id", pattern queries with wildcards
- Operations: `add`, `query`, `invalidate`, `timeline`

**Phase 7 — MCP Server**
- Model Context Protocol server (ModelContextProtocol 1.2.0)
- 29 tools: palace read/write, KG ops, agent diary access
- VS Code / Claude Desktop / MCP Inspector integration

**Phase 8 — Agent Framework**
- Microsoft.Agents.AI 1.3.0 integration
- Per-agent wings + memory diaries
- `mempalace_list_agents` discoverability
- YAML-based agent configuration

**Phase 9 — Benchmarks**
- LongMemEval / LoCoMo / ConvoMem harness support
- Synthetic dataset generation
- R@5 metric validation (target ≥ 91%)
- Reproducibility scripts + documentation

**Phase 10 — Polish**
- NuGet package metadata for all libraries
- Release-quality README
- Consolidated documentation index
- CI workflow hardening (pack job on tags)

### Known Limitations

- SQLite vector store uses O(n) brute-force cosine similarity (acceptable for <100K vectors; upgrade to sqlite-vec or Qdrant planned post-v0.1)
- Keyword search uses token overlap (BM25 planned post-v0.1)
- Conversation context summaries (`mempalace wake-up`) not yet implemented

[0.1.0]: https://github.com/elbruno/mempalacenet/releases/tag/v0.1.0
