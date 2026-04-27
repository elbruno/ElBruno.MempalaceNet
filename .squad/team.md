# Squad Team

> elbruno.mempalacenet

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Deckard | 🏗️ Lead / Architect | agents/deckard/charter.md | active |
| Tyrell | 🔧 Core Engine Dev | agents/tyrell/charter.md | active |
| Roy | 🤖 AI / Agent Integration | agents/roy/charter.md | active |
| Rachael | ⚛️ CLI / UX Dev | agents/rachael/charter.md | active |
| Bryant | 🧪 Tester / QA | agents/bryant/charter.md | active |
| Scribe | 📋 Session Logger | agents/scribe/charter.md | active |
| Ralph | 🔄 Work Monitor | agents/ralph/charter.md | active |

## Project Context

- **User:** Bruno Capuano
- **Project:** MemPalace.NET — .NET port of [MemPalace](https://github.com/MemPalace/mempalace)
- **Goal:** Local-first AI memory in .NET. Verbatim storage, semantic search, wings/rooms/drawers hierarchy, pluggable backends, CLI + MCP server.
- **Stack:** .NET (latest), Microsoft.Extensions.AI, Microsoft Agent Framework, official .NET libraries.
- **Repo:** github.com/elbruno/mempalacenet (private, MIT)
- **Created:** 2026-04-24

## Project Rules
1. **Constant pushes** — commit and push to GitHub frequently after every meaningful unit of work.
2. **Docs location** — all documentation under `docs/`. Only `README.md` and `LICENSE` may sit at repo root.
3. **Code location** — all source code under `src/`.

## Strategic Focus

**Current Phase:** v0.7.0 Kickoff (Agent Workflows & Integrations) — Ready for Implementation

**v0.7.0 Launch Date:** 2026-04-27 (Deckard validation + team consensus complete)

**v0.7.0 Roadmap (8-10 weeks):**
  - P0: MCP SSE Transport (Tyrell) + LLM Wake-Up (Roy) parallel, Weeks 1-3
  - P0: Embedder Pluggability (Tyrell), Weeks 1-2
  - P1: Skill Marketplace CLI (Rachael), Weeks 2-3
  - P1: Test Coverage Strategy (Bryant), Weeks 1-4 (parallel with dev)
  - P2: Release prep + integration (Deckard), Week 8-10

**Parallel Workstreams:** All 5 decisions architecturally validated. Zero blockers. Teams ready to start immediately.

**Prior Phase:** v0.6.0 Implementation ✅ COMPLETE (sqlite-vec, BM25, LongMemEval validation, Copilot Skill PR #1 merged)

**Next Milestone:** v0.7.0 Phase 12 kickoff → skill delivery in 8-10 weeks
