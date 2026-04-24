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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
