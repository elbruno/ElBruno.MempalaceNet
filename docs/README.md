# MemPalace.NET Documentation

All project documentation lives under this folder.

## Index

### Core Concepts
- [PLAN.md](PLAN.md) — phased implementation plan
- [architecture.md](architecture.md) — solution layout, component contracts, dependency graph

### Backends & Storage
- [backends.md](backends.md) — backend interface, writing custom backends, conformance tests

### AI & Embeddings
- [ai.md](ai.md) — Microsoft.Extensions.AI integration, embedder selection, reranking

### Content & Search
- [mining.md](mining.md) — ingestion pipeline, filesystem + conversation miners
- [search.md](search.md) — semantic vs hybrid strategies, RRF fusion, temporal boosting

### Knowledge Graph
- [kg.md](kg.md) — temporal entity-relationship triples, pattern queries, validity windows

### Integrations
- [mcp.md](mcp.md) — Model Context Protocol server (29 tools), VS Code / Claude Desktop setup
- [agents.md](agents.md) — Microsoft Agent Framework integration, per-agent diaries, discovery

### Tools & Benchmarks
- [cli.md](cli.md) — command reference (`mempalacenet init`, `mine`, `search`, `agents`, `kg`, `mcp`)
- [benchmarks.md](benchmarks.md) — LongMemEval / LoCoMo / ConvoMem harnesses, R@5 validation

### Release
- [CHANGELOG.md](CHANGELOG.md) — version history
- [RELEASE-v0.1.md](RELEASE-v0.1.md) — v0.1.0 release notes
