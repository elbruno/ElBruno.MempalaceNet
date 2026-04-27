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

**Current Phase:** v0.7.0 Phase 2 (Integration & Optimization) — ACTIVE 🚀

**Phase 1 Status:** ✅ COMPLETE (MCP SSE Transport, Wake-up LLM, Skill CLI Phase 1)

**Phase 2 Timeline:** Weeks 2-3 (2026-04-28 → 2026-05-08)

**Phase 2 Workstreams (Parallel):**
  - Workstream A: CLI Integration (Rachael + Tyrell) — 5-7 days
  - Workstream B: MCP Tool Expansion (Roy) — 4-6 days  
  - Workstream C: Backend Optimization (Tyrell) — 2-3 days

**Phase 3 Timeline:** Weeks 4-5 (2026-05-08 → 2026-05-20)
  - Workstream D: Embedder Interface (Tyrell + Roy) — 3-4 days
  - Workstream E: Release Prep (Deckard + Bryant) — 5-7 days

**Roadmap:** See `docs/guides/v070-phase2-phase3-roadmap.md` for full dependency graph and task breakdown.

**Prior Phase:** v0.6.0 Implementation ✅ COMPLETE (sqlite-vec, BM25, LongMemEval validation, Copilot Skill PR #1 merged)

**Next Milestone:** v0.7.0 Phase 12 kickoff → skill delivery in 8-10 weeks
