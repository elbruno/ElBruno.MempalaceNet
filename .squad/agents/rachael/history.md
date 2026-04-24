# Rachael — History

## Core Context
- **Project:** MemPalace.NET — port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** CLI / UX
- **Reference CLI:** `mempalace init`, `mempalace mine`, `mempalace search`, `mempalace wake-up`. Plus `mempalace mine ~/.claude/projects/ --mode convos --wing X`.
- **Tooling:** Spectre.Console + Spectre.Console.Cli for rich terminal UI.

## Learnings

### 2026-04-24: Phase 5 CLI Scaffold Complete

**What:** Implemented complete CLI surface using Spectre.Console.Cli with Microsoft.Extensions.Hosting for DI.

**Commands Implemented:**
- Root commands: init, mine, search, wake-up
- Agent branch: agents list
- KG branch: kg add, query, timeline

**Key Decisions:**
- Used Spectre.Console.Cli over System.CommandLine (per PLAN.md)
- Implemented custom TypeRegistrar/TypeResolver to bridge Spectre with M.E.DI
- All handlers return stub implementations with TODO markers for future phases
- Rich output with panels, tables, progress bars using Spectre.Console

**Technical Highlights:**
- DI integration allows future injection of IBackend, IEmbedder from other phases
- Configuration loading from mempalace.json + env vars (MEMPALACE_*)
- Proper command branching for nested commands (agents, kg)
- 10 parse tests confirm all command routing works correctly

**Blockers:**
- Full solution build blocked by MemPalace.Ai compile errors (Roy's Phase 3 work)
- MemPalace.Cli builds independently and all commands execute successfully
- Tests exist but can't run until Ai project compiles

**Artifacts:**
- src/MemPalace.Cli/Commands/ - All command implementations
- src/MemPalace.Cli/Infrastructure/ - DI bridge for Spectre
- src/MemPalace.Tests/Cli/CommandAppParseTests.cs - Parse verification tests
- docs/cli.md - Complete CLI reference documentation

**Next:** Ready for Phase 4 teams (Tyrell + Roy) to wire in real backend/embedder implementations.
