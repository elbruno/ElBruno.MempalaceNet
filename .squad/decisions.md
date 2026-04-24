# Squad Decisions

## Active Decisions

### 2026-04-24: Project rules
**By:** Bruno Capuano (via Copilot)
**What:** (1) Constant pushes to GitHub. (2) All docs under `docs/` — only `README.md` and `LICENSE` at root. (3) All code under `src/`.

### 2026-04-24: Tech stack
**By:** Bruno Capuano (via Copilot)
**What:** Target latest .NET. Use Microsoft.Extensions.AI for embeddings/LLM abstractions, Microsoft Agent Framework for agent layer, official .NET libraries throughout.

### 2026-04-24: Repository
**By:** Bruno Capuano (via Copilot)
**What:** Private GitHub repo `elbruno/mempalacenet`, MIT license. Reference port of https://github.com/MemPalace/mempalace.

### 2026-04-24: Solution layout
**By:** Deckard (proposed)
**What:** `src/MemPalace.Core` (domain + storage interfaces), `src/MemPalace.Backends.Sqlite` (default backend, sqlite-vec), `src/MemPalace.Ai` (M.E.AI embedder + reranker), `src/MemPalace.Cli` (Spectre.Console.Cli), `src/MemPalace.Mcp` (MCP server), `src/MemPalace.Agents` (Agent Framework integration), `tests/MemPalace.Tests`.

## Phase 0 (Deckard — Lead)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Deckard | Target Framework & Project Graph | net10.0, project graph with Core as root. Solution `.slnx`. | Latest .NET, CI supports preview. Clean dependency order. |

## Phase 1 (Tyrell — Core Contract)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Tyrell | Core Contract — IBackend, ICollection, IEmbedder | Immutable records, ReadOnlyMemory<float>, nested QueryResult vs flat GetResult. | Functional domain, zero-copy vectors, batch query support. |

## Phase 2 (Tyrell — Storage Backend)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Tyrell | SQLite Backend Vector Storage | Pure managed BLOB + brute-force cosine similarity (O(n) search). | No stable sqlite-vec NuGet; acceptable perf for <100K records; clear upgrade path to Qdrant/Chroma. |

## Phase 3 (Roy — AI Integration)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Roy | M.E.AI Integration | M.E.AI 9.5.0, Abstractions 10.3.0, Ollama preview 9.1.0-preview.1.25064.3. OpenAI/Azure deferred. | Ollama is local-first default (no API keys); preview version stable enough; OpenAI APIs not yet compatible. |

## Phase 4 (Tyrell — Mining & Search)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Tyrell | Mining Architecture | IMiner + MiningPipeline (separation of concerns); FileSystemMiner with .gitignore + 2000-char chunks. | Extensible (pluggable miners); streaming (no buffering); .gitignore respects user intent. |
| 2026-04-24 | Tyrell | Conversation Mining | Support JSONL + Markdown transcripts. Format auto-detected by extension. | Ubiquitous export formats (Claude, ChatGPT, Copilot); robust regex handling. |
| 2026-04-24 | Tyrell | Search Strategy | Vector search + hybrid (RRF with k=60). Token overlap for keywords (v0.1, not BM25). | Different use cases (semantic vs exact term); pragmatic scoring until >10K docs. |
| 2026-04-24 | Tyrell | Reranking | Optional IReranker, opt-in via SearchOptions.Rerank flag. | Phase 5 concern (LLM cost); flexibility for implementations. |
| 2026-04-24 | Tyrell | Miner DI | Keyed services ("filesystem", "conversation"). | Discovery + extensibility; users can add custom miners. |

## Phase 5 (Rachael — CLI)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Rachael | CLI Framework | Spectre.Console.Cli with TypeRegistrar/TypeResolver DI integration. | Production-ready, rich output, clean attribute-based settings, excellent docs. |

## Phase 6 (Roy — Knowledge Graph)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Roy | Temporal Knowledge Graph | SQLite backend, temporal triples (ValidFrom/ValidTo + RecordedAt). EntityRef = "type:id", pattern queries with wildcards. | Captures relationship validity over time; SQLite proven; ISO8601 UTC for portability; wildcard queries powerful for exploration. |

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
