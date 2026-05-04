# MemPalace.NET

[![CI](https://github.com/elbruno/ElBruno.MempalaceNet/actions/workflows/ci.yml/badge.svg)](https://github.com/elbruno/ElBruno.MempalaceNet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v0.15.1-blue.svg)](https://www.nuget.org/packages/mempalacenet)
[![Tests](https://img.shields.io/badge/Tests-152%2F152%20passing-brightgreen.svg)](#)

A **.NET port** of [MemPalace](https://github.com/MemPalace/mempalace) — local-first AI memory that stores everything verbatim, searches semantically, and organizes knowledge through a *wings / rooms / drawers* hierarchy. No cloud calls by default, powered by ONNX embeddings.

> 🎯 **Status:** v0.15.1 — Production-ready with advanced E2E testing, comprehensive journey guides, and skill pattern library.

## Why MemPalace.NET?

- **Local-first by default** — ONNX embeddings via [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings) (no API keys, no cloud calls)
- **Microsoft.Extensions.AI** — swap embedders and LLMs with zero lock-in
- **Microsoft Agent Framework** — each agent gets its own memory diary
- **MCP server** — expose your palace as Model Context Protocol tools (Claude Desktop, VS Code, etc.)
- **Temporal knowledge graph** — track entity relationships with validity windows
- **SQLite backend** — managed BLOB storage, cosine similarity, clear upgrade path to vector stores

## Examples & Getting Started

Ready to dive in? Check out our **[runnable examples](./examples/README.md)**:

- 🔰 **[Simple Memory Agent](./examples/SimpleMemoryAgent/)** — Core memory operations with semantic search
- 🕸️ **[Semantic Knowledge Graph](./examples/SemanticKnowledgeGraph/)** — Temporal entity relationships

See [examples/README.md](./examples/README.md) for detailed walkthroughs and learning paths.

## Quick Start

```bash
# Install the CLI tool
dotnet tool install -g mempalacenet --version 0.15.1

# Initialize a new palace
mempalacenet init ~/my-palace

# Mine project files
mempalacenet mine ~/my-code --wing work --mode files

# Mine conversation transcripts
mempalacenet mine ~/my-convos --wing personal --mode convos

# Semantic search
mempalacenet search "how do I handle auth errors?"

# Hybrid search with reranking
mempalacenet search "latest React patterns" --hybrid --rerank

# Start MCP server (for Claude Desktop, VS Code, etc.)
mempalacenet mcp --palace ~/my-palace

# Run an agent
mempalacenet agents run scribe --wing research --mode local
```

## Architecture

MemPalace.NET is a modular .NET solution with clear separation of concerns:

| Project | Purpose |
|---------|---------|
| **MemPalace.Core** | Domain types, storage interfaces, PalaceRef value object |
| **MemPalace.Backends.Sqlite** | Default SQLite backend with BLOB vectors + cosine similarity |
| **MemPalace.Ai** | M.E.AI integration with ONNX (default), Ollama, OpenAI support |
| **MemPalace.Mining** | Content ingestion: filesystem miner + conversation transcript miner |
| **MemPalace.Search** | Semantic, keyword, and hybrid search with optional LLM reranking |
| **MemPalace.KnowledgeGraph** | Temporal entity-relationship graph with validity windows |
| **MemPalace.Mcp** | Model Context Protocol server (7 tools in v0.1) |
| **MemPalace.Agents** | Microsoft Agent Framework integration + per-agent diaries |
| **MemPalace.Cli** | Spectre.Console CLI (`mempalacenet` command) |
| **MemPalace.Benchmarks** | LongMemEval / LoCoMo / ConvoMem benchmarks + R@5 testing |

## Documentation

Full documentation lives in [`docs/`](docs/):

- **[Architecture](docs/architecture.md)** — solution layout, component contracts, dependency graph
- **[Concepts](docs/PLAN.md)** — wings, rooms, drawers, verbatim storage, embedder identity
- **[Backends](docs/backends.md)** — writing custom backends, conformance tests
- **[AI Integration](docs/ai.md)** — embedder selection, reranking, M.E.AI seams
- **[Mining](docs/mining.md)** — ingestion pipeline, custom miners, .gitignore respect
- **[Search](docs/search.md)** — semantic vs hybrid strategies, RRF fusion, temporal boosting
- **[Knowledge Graph](docs/kg.md)** — temporal triples, pattern queries, invalidation
- **[MCP Server](docs/mcp.md)** — tool reference, VS Code / Claude Desktop setup
- **[Agents](docs/agents.md)** — Agent Framework integration, diary management, agent discovery
- **[CLI](docs/cli.md)** — command reference, configuration, examples
- **[Benchmarks](docs/benchmarks.md)** — reproducibility, dataset sources, R@5 parity
- **[GitHub Copilot Skill](docs/COPILOT_SKILL.md)** — integration guide, pattern library, code generation hints

## Building Custom Integrations

Integrating MemPalace.NET into your .NET projects? Start with our developer guides:

- **[C# Library Developer Guide](docs/guides/csharp-library-developers.md)** — Build applications on MemPalace.NET
- **[Embedder Pluggability Guide](docs/guides/embedder-pluggability.md)** — Swap or implement custom embedders
- **[Skill Integration Deep Dive](docs/guides/skill-integration-deep-dive.md)** — Extend the platform with reusable skills

## Development

```bash
# Clone
git clone https://github.com/elbruno/mempalacenet
cd mempalacenet

# Build
dotnet build src/

# Test (129 tests, all green)
dotnet test src/

# Pack NuGet packages
dotnet pack src/ -c Release
```

## Roadmap

**v0.1.0** (current) ships core memory operations, search, MCP server, and agents.

**Post-v0.1:**
- Upgrade to [sqlite-vec](https://github.com/asg017/sqlite-vec) or Qdrant for >100K vectors
- BM25 keyword search (currently token overlap)
- LongMemEval R@5 parity validation (target ≥ 91%)
- Conversation context summaries (`mempalace wake-up`)

## Credits

- **Original project:** [MemPalace](https://github.com/MemPalace/mempalace) (Python)
- **Default embedder:** [ElBruno.LocalEmbeddings](https://github.com/elbruno/LocalEmbeddings) (ONNX)

## License

[MIT](LICENSE) — same spirit as the original MemPalace.

## 👋 About the Author

**Made with ❤️ by [Bruno Capuano (ElBruno)](https://github.com/elbruno)**

- 📝 **Blog**: [elbruno.com](https://elbruno.com)
- 📺 **YouTube**: [youtube.com/elbruno](https://youtube.com/elbruno)
- 🔗 **LinkedIn**: [linkedin.com/in/elbruno](https://linkedin.com/in/elbruno)
- 𝕏 **Twitter**: [twitter.com/elbruno](https://twitter.com/elbruno)
- 🎙️ **Podcast**: [notienenombre.com](https://notienenombre.com)

---

## Community

We welcome contributions from the community! Here's how to get involved:

- **[Contributing Guidelines](.github/CONTRIBUTING.md)** — how to submit pull requests, report issues, and contribute code
- **[Code of Conduct](.github/CODE_OF_CONDUCT.md)** — our commitment to fostering an open and welcoming environment
- **[Security Policy](.github/SECURITY.md)** — how to report security vulnerabilities responsibly
- **[Issues](https://github.com/elbruno/mempalacenet/issues)** — report bugs or request features
- **[Discussions](https://github.com/elbruno/mempalacenet/discussions)** — ask questions, share ideas, and connect with the community

**Got questions?** Open a [discussion](https://github.com/elbruno/mempalacenet/discussions) or reach out to [@elbruno](https://github.com/elbruno).
