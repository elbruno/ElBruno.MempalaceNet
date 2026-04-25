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

## Phase 7 (Roy — MCP Server)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Roy | MCP Package Selection | Use ModelContextProtocol v1.2.0 (stable, net10.0 supported). Upgrade Microsoft.Extensions.Hosting to 10.0.7. | v1.2.0 is stable and fully compatible; eliminates preview risks. Hosting upgrade has zero breaking changes. |
| 2026-04-24 | Roy | MCP Transport | Implement stdio (fully functional). Defer SSE/HTTP to future phase. CLI flag `--transport sse` returns helpful error. | Stdio is sufficient for Claude Desktop, VS Code, Copilot CLI, MCP Inspector. HTTP hosting can wait until needed. |
| 2026-04-24 | Roy | Tool Surface | Deliver 7 tools: palace_search, palace_recall, palace_get, palace_list_wings, kg_query, kg_timeline, palace_health. Defer write operations to Phase 8. | Covers read operations and palace discovery. Write operations require agent framework integration and write policy. |

## Phase 8 (Roy — Agent Framework Integration v2)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-25 | Roy | Agent Framework Package | Use Microsoft.Agents.AI 1.3.0 (stable, verified build-clean). Restore from initial removal due to false conflict detection. | Package is stable and fully compatible. Bruno verified restore and build succeed. Previous removal was based on incorrect assumptions about package conflicts. |
| 2026-04-25 | Roy | ChatClientAgent Integration | Use Microsoft.Agents.AI `ChatClientAgent` wrapper with M.E.AI `IChatClient`. Provide `MemPalaceAgent` thin wrapper. | Production-ready tool orchestration and structured responses. `ChatClientAgent` is the correct abstraction for LLM-backed agents with tools. |
| 2026-04-25 | Roy | Tool Wiring | Convert MCP tools (palace_search, kg_query) to `AIFunction`s via `AIFunctionFactory.Create()`. Wire at agent build time. | M.E.AI `AIFunction` abstracts tool invocation, compatible with function calling. Wired at build time for clarity (dynamic discovery deferred). |
| 2026-04-25 | Roy | Agent Diary | Store entries as embeddings in palace backend (collection: agent_diary:{agentId}). Record user + assistant messages + metadata. | Reuses existing backend + embedder + search infrastructure. Enables semantic search over conversation history. Persistent across sessions. |

## Phase 9 (Bryant — Benchmark Harness)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Bryant | Benchmark Architecture | Interface-based design: IBenchmark, BenchmarkContext, BenchmarkResult, BenchmarkBase (shared scoring logic). | Clean abstraction for adding new benchmarks. Type-safe context passing. DRY scoring via base class. |
| 2026-04-24 | Bryant | Dataset Loading | JSONL streaming via IAsyncEnumerable<DatasetItem>. Line-by-line parsing with maxItems limit for smoke tests. | Supports large datasets (10k+) without buffering. Optional limits for quick CI smoke tests. Standard JSONL format. |
| 2026-04-24 | Bryant | Scoring Metrics | Pure functions in Metrics class: Recall@k, Precision@k, F1, NDCG@k. NDCG uses log2(position+2) for 1-indexed positions. | Stateless functions, easy to test. Standard IR metrics for memory retrieval. NDCG accounts for ranking quality. |
| 2026-04-24 | Bryant | Micro-Benchmarks | Use BenchmarkDotNet for embedding throughput and query latency. Run in Release mode. Separate from functional benchmarks. | Industry-standard .NET tool. Automatic warmup, statistical analysis, memory diagnostics. Different concerns from functional benchmarks. |
| 2026-04-24 | Bryant | Synthetic Datasets | Hand-rolled 5-item JSONL fixtures under datasets-synthetic/ for CI. Real datasets (100MB+) obtained separately by Bruno. | CI shouldn't download large datasets or call real models. Fixtures verify harness mechanics without requiring model/data setup. |
| 2026-04-24 | Bryant | CLI Design | Spectre.Console.Cli with commands: list, run, run-all, micro. Attribute-based settings (type-safe, self-documenting). | Consistent with main mempalacenet CLI. Short imperative verb names (git/kubectl style). Rich terminal output. |

## Phase 10 (Deckard — Release Prep)

| Date | Agent | Scope | Decision | Rationale |
|------|-------|-------|----------|-----------|
| 2026-04-24 | Deckard | NuGet Metadata | Consolidate common metadata in Directory.Build.props (version, author, license, repo). Per-project: PackageId, Description, README include. | DRY principle. Discoverability via PackageTags. ToolCommandName for CLI packages. Consistent across 10 packages. |
| 2026-04-24 | Deckard | README Rewrite | Concise pitch, quick start commands, architecture table, documentation links, roadmap, credits, license. | First impression matters. Action-oriented. Links to docs reduce duplication. |
| 2026-04-24 | Deckard | Documentation Structure | Create docs/CHANGELOG.md (phases + known limits), docs/RELEASE-v0.1.md (highlights + getting started), docs/README.md (topical index). | Clear release history. GitHub release template ready. Organized documentation. |
| 2026-04-24 | Deckard | CI Pack Job | Add pack job to ci.yml: runs on main pushes and v* tags. No auto-publish to NuGet. | Automate package verification. Tag releases trigger packing. Manual publish control prevents accidental releases. |
| 2026-04-24 | Deckard | Phase Status Tracking | Update docs/PLAN.md with commit SHAs for each completed phase. Emoji indicators (✅ Done, 🚧 In progress). | Historical record. Easy to reference specific commits. Clear status visibility. |

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
