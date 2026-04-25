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

## Strategic Focus (Post-v0.5.0 → v0.6.0)

**Current Phase:** v0.6.0 Implementation (Production-Grade Search Foundation)

**Skill Status:** PR #1 open for team review; publish post-v0.6.0

**v0.6.0 Roadmap:** 
  - P0: sqlite-vec (Tyrell) + BM25 (Roy) parallel, Weeks 1-4
  - P1: LongMemEval R@5 validation (Bryant), Weeks 5-8
  - P2: Release prep (Deckard), Week 9
  - P2+: Copilot Skill publication, Weeks 10+

**Next Milestone:** Team approval of research recommendations → Spike PRs
