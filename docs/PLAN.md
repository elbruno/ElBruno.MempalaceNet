# MemPalace.NET — Implementation Plan

A phased plan to port [MemPalace](https://github.com/MemPalace/mempalace) (Python) to .NET, using Microsoft.Extensions.AI and Microsoft Agent Framework.

---

## Project Rules (apply to every phase)
1. **Constant pushes** — commit and push to GitHub frequently.
2. **Docs location** — all documentation under `docs/`. Only `README.md` and `LICENSE` may sit at repo root.
3. **Code location** — all source code under `src/`.

---

## Target Architecture

```
src/
├── MemPalace.Core/              # Domain types + storage interfaces
│   ├── Model/                   # PalaceRef, Wing, Room, Drawer, Memory
│   ├── Backends/                # IBackend, ICollection, QueryResult
│   ├── Errors/                  # BackendError hierarchy
│   └── Palace.cs                # Public entry point
├── MemPalace.Backends.Sqlite/   # Default backend (Microsoft.Data.Sqlite + sqlite-vec)
├── MemPalace.Ai/                # Microsoft.Extensions.AI integration
│   ├── Embedding/               # IEmbedder over IEmbeddingGenerator<>
│   └── Rerank/                  # LLM-based reranker
├── MemPalace.KnowledgeGraph/    # Temporal entity-relationship graph (SQLite)
├── MemPalace.Mcp/               # MCP server (ModelContextProtocol package)
├── MemPalace.Agents/            # Microsoft Agent Framework integration + agent diaries
├── MemPalace.Cli/               # Spectre.Console.Cli — `mempalace` command
└── MemPalace.Tests/             # xUnit, FluentAssertions, NSubstitute

docs/
├── PLAN.md                      # this file
├── architecture.md              # solution layout + component contracts
├── concepts.md                  # palace / wings / rooms / drawers
├── cli.md                       # CLI reference
├── backends.md                  # backend interface + writing custom backends
├── ai.md                        # M.E.AI integration, embedder selection
├── mcp.md                       # MCP tool reference
└── benchmarks.md                # parity benchmarks
```

---

## Phase 0 — Scaffold & Repo (active)
**Owner:** Deckard
- [x] Squad team set up
- [x] LICENSE (MIT), README, .gitignore, .gitattributes
- [x] `docs/PLAN.md`
- [ ] Create private repo `elbruno/mempalacenet`
- [ ] First push (scaffold only)
- [ ] `MemPalace.sln` with empty project stubs
- [ ] CI workflow: build + test on push (`.github/workflows/ci.yml`)

**Exit criteria:** repo exists on GitHub, CI green on empty solution.

---

## Phase 1 — Core Domain & Backend Contract
**Owner:** Tyrell · Reviewer: Deckard, Bryant
- Port `BaseBackend` / `BaseCollection` / `QueryResult` / `GetResult` / errors → C# interfaces and records (`MemPalace.Core`)
- Port `PalaceRef` value object
- Define `IEmbedder` seam (lives in Core; implementation in `MemPalace.Ai`)
- Backend conformance test suite (xUnit theory) usable by any backend implementation
- In-memory backend for tests

**Exit criteria:** Conformance suite green against in-memory backend.

---

## Phase 2 — SQLite Backend (default)
**Owner:** Tyrell · Reviewer: Deckard, Bryant
- `MemPalace.Backends.Sqlite` using `Microsoft.Data.Sqlite`
- Vector storage: try [`sqlite-vec`](https://github.com/asg017/sqlite-vec) extension; fallback to BLOB column + brute-force cosine
- Collection schema with embedder identity stored alongside vectors
- Migration helpers
- Conformance suite green against SQLite backend

**Open question for Bruno:** preference between `sqlite-vec` (newer, fast) vs. `Microsoft.SemanticKernel.Connectors.Sqlite` (Microsoft-stewarded but heavier dep)? Default plan: `sqlite-vec`.

---

## Phase 3 — AI Integration (embeddings + rerank)
**Owner:** Roy · Reviewer: Deckard
- `MemPalace.Ai` wraps `Microsoft.Extensions.AI`'s `IEmbeddingGenerator<string, Embedding<float>>`
- Default provider: **Ollama** (`Microsoft.Extensions.AI.Ollama`) with `nomic-embed-text`
- Optional providers: OpenAI, Azure OpenAI (config-gated)
- Embedder identity guard (matches Python `EmbedderIdentityMismatchError`)
- Optional LLM reranker for top-K → top-1 (any `IChatClient`)

**Exit criteria:** can embed text via Ollama and store/query through Tyrell's SQLite backend.

---

## Phase 4 — Mining & Search Pipeline
**Owners:** Tyrell + Roy
- Ingestion: project files (markdown, code), conversation transcripts (Claude / generic JSONL)
- Auto-routing into wings/rooms/drawers with explicit `--wing` override
- Search: raw semantic, hybrid (keyword boosting + temporal proximity + preference patterns), optional rerank
- Match Python's hybrid v4 / v5 strategy as closely as practical

**Exit criteria:** search-quality smoke tests pass; small benchmark slice runs end-to-end.

---

## Phase 5 — CLI
**Owner:** Rachael · Reviewer: Deckard
- `mempalace init <path>` — initialize palace
- `mempalace mine <path> [--mode files|convos] [--wing X]`
- `mempalace search <query> [--wing X] [--rerank]`
- `mempalace wake-up` — load context summary for new session
- `mempalace agents list`, `mempalace kg add/query/timeline`
- Spectre.Console for output

**Exit criteria:** all CLI commands functional against a real palace.

---

## Phase 6 — Knowledge Graph
**Owner:** Tyrell + Roy · Reviewer: Deckard
- Temporal entity-relationship graph (SQLite-backed)
- Validity windows per relationship
- Operations: `add`, `query`, `invalidate`, `timeline`

**Exit criteria:** KG schema migrated, CRUD covered by tests.

---

## Phase 7 — MCP Server
**Owner:** Roy · Reviewer: Deckard
- `MemPalace.Mcp` using `ModelContextProtocol` package
- Initial 10–12 tools (palace read/write, KG ops, agent diary read)
- Then expand toward parity with Python's 29-tool surface

**Exit criteria:** MCP server callable from VS Code / Claude Desktop / MCP Inspector.

---

## Phase 8 — Agent Framework Integration
**Owner:** Roy · Reviewer: Deckard
- `MemPalace.Agents` using **Microsoft Agent Framework** (`Microsoft.Agents.AI`)
- Each specialist agent gets its own wing + diary in the palace
- `mempalace_list_agents` discoverability

**Exit criteria:** sample agent can read/write its diary via the framework.

---

## Phase 9 — Benchmarks & Parity
**Owner:** Bryant · Reviewer: Deckard
- LongMemEval harness (R@5)
- LoCoMo harness (R@10)
- ConvoMem / MemBench harnesses
- Reproducibility scripts under `src/MemPalace.Benchmarks/` + results in `docs/benchmarks/`

**Exit criteria:** raw R@5 within 5 percentage points of Python reference (target ≥ 91% if .NET embedder differs).

---

## Phase 10 — Polish & v0.1
**Owner:** Deckard
- README hardened, docs/ complete, NuGet packaging metadata
- Tag `v0.1.0`, GitHub release notes

---

## Hard Questions to Watch
- **Vector backend choice** (Phase 2): `sqlite-vec` vs SK connector vs Qdrant.NET.
- **Embedder default** (Phase 3): Ollama nomic-embed-text vs an Onnx local model bundled via `Microsoft.ML.OnnxRuntime` (zero external runtime).
- **CLI framework** (Phase 5): Spectre.Console.Cli vs `System.CommandLine` (Microsoft official, but still preview-ish).
- **MCP package** (Phase 7): the `ModelContextProtocol` NuGet is preview — confirm versioning rules.

These get raised explicitly at the start of their phase and will pause for Bruno's input if not pre-decided.
