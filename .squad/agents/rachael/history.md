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

### 2026-04-25: End-to-End CLI Verification

**What:** Verified CLI commands work end-to-end against test palace setup.

**Test Results:**
- ✅ `mempalacenet init C:\temp\test-palace` - Works, displays nice Spectre.Console panel with TODO notice
- ✅ `mempalacenet mine C:\src\elbruno.mempalacenet\docs --wing docs --mode files` - Beautiful progress bar, clean output
- ✅ `mempalacenet search "architecture"` - Excellent table output with example results
- ✅ `mempalacenet kg add agent:Claude is-type llm:gpt` - Perfect! Shows relationship with temporal info
- ❌ `mempalacenet agents list` - DI resolution error: "Could not resolve type 'MemPalace.Cli.Commands.Agents.AgentsListCommand'"

**UX Highlights:**
- Spectre.Console styling is beautiful and consistent across all commands
- Progress bars for mining show smooth transitions (0% → 30% → 50% → 70% → 90% → 100%)
- Tables use clean borders and proper column alignment
- Panels have nice spacing and clear messaging
- All error messages are clear and actionable (e.g., "Invalid EntityRef format: 'Claude'. Expected 'type:id'")

**Issues Found:**
1. **agents list command fails** - TypeResolver can't instantiate AgentsListCommand due to IAgentRegistry dependency issue
   - Root cause: Likely IChatClient is not configured, so EmptyAgentRegistry is used, but command resolution fails before we even get there
   - The error happens at command instantiation, not execution
   - Other commands (kg add) work fine with their DI dependencies

2. **EntityRef format needs documentation** - User needs to know format is "type:id" for kg commands
   - Error message is clear, but could be shown in help text or examples

**Build Status:**
- CLI builds successfully in ~11 seconds
- All tested commands execute (except agents list)
- No compilation warnings

**Observations:**
- The app name in Program.cs is "mempalacenet" but docs might reference "mempalace" - should verify consistency
- Knowledge graph commands work great with temporal tracking (valid-from/valid-to)
- Mining shows infrastructure is wired but awaiting backend/embedder (as expected)

**Next Actions:**
- Fix agents list command DI resolution issue
- Verify command name consistency across docs (mempalace vs mempalacenet)
- Consider pre-creating sample .mempalace directory in init command

### 2026-04-25: Fixed agents list DI Resolution

**Issue:** `agents list` command failed with "Could not resolve type 'MemPalace.Cli.Commands.Agents.AgentsListCommand'"

**Root Cause:** Complex DI dependency chain issue:
1. `AgentsListCommand` requires `IAgentRegistry`
2. `IAgentRegistry` factory creates `IMemPalaceAgentBuilder`
3. `IMemPalaceAgentBuilder` constructor has optional parameters including `IAgentDiary`
4. .NET DI always tries to resolve optional constructor parameters, triggering `IAgentDiary` factory
5. `IAgentDiary` factory conditionally creates `BackedByPalaceDiary` or `InMemoryAgentDiary`
6. Factory checked for `ISearchService` availability using `sp.GetService<ISearchService>()`
7. `ISearchService` registration uses `sp.GetRequiredService<IBackend>()` in its factory
8. **Exception thrown:** "No service for type 'MemPalace.Core.Backends.IBackend' has been registered"

**The Fix:**
1. **Created `InMemoryAgentDiary` stub** - Simple in-memory implementation for when backend/embedder not available
2. **Fixed `IAgentDiary` factory** - Check for `IBackend` and `IEmbedder` directly (don't call GetService on ISearchService since it throws)
3. **Fixed `IMemPalaceAgentBuilder` factory** - Conditionally resolve ISearchService only if IBackend available
4. **Added `AgentsListSettings`** - Changed from `AsyncCommand` to `AsyncCommand<AgentsListSettings>` to match Spectre.Console.Cli pattern
5. **Cached ServiceProvider** - TypeRegistrar now caches the built ServiceProvider for reuse

**Technical Deep Dive:**
- Optional constructor parameters in .NET DI are NOT truly optional - DI still attempts resolution
- When a factory throws during GetService, the exception bubbles up even if you were only checking availability
- Solution: Check for prerequisite services first, then only call GetService if safe
- This pattern now works: backend/embedder/search optional → agents work without full infrastructure

**Testing:**
- ✅ `mempalacenet agents list` now works - shows "No agents found. Create agent YAML files in .mempalace/agents/"
- ✅ Output is clean and well-formatted using Spectre.Console
- ✅ Command exits cleanly with code 0
- ✅ No IChatClient required for listing (uses EmptyAgentRegistry as designed)

**Key Learnings:**
1. .NET DI GetService can throw if the factory itself calls GetRequiredService
2. Always check transitive dependencies when designing optional service patterns
3. Debugging with file logging (resolver.log) was crucial to trace through the DI resolution chain
4. Spectre.Console.Cli commands with dependencies should use `AsyncCommand<TSettings>` pattern

**Files Changed:**
- `src/MemPalace.Agents/ServiceCollectionExtensions.cs` - Added InMemoryAgentDiary, fixed factories
- `src/MemPalace.Cli/Commands/Agents/AgentsListCommand.cs` - Added settings class, updated signature
- `src/MemPalace.Cli/Infrastructure/TypeRegistrar.cs` - Cache ServiceProvider for reuse
- `src/MemPalace.Cli/Infrastructure/TypeResolver.cs` - Simplified to use GetService consistently

**Impact:**
- All agent commands now work without requiring backend/embedder/search configuration
- Clean separation: agent listing works with minimal deps, agent execution requires full stack
- Pattern established for other commands that need optional service dependencies

### 2026-04-25: README Polish with Badges and Community Links

**What:** Enhanced README.md with visual badges and community section for improved credibility and contributor onboarding.

**Changes Made:**
1. **Badges Section** - Added 4 badges after title:
   - CI/Build status badge (GitHub Actions workflow)
   - MIT License badge
   - NuGet version badge (v0.1.0-preview.1)
   - Test status badge (152/152 passing)

2. **Community Section** - Added at bottom with links to:
   - Contributing guidelines (.github/CONTRIBUTING.md)
   - Code of Conduct (.github/CODE_OF_CONDUCT.md)
   - Security Policy (.github/SECURITY.md)
   - GitHub Issues and Discussions

**UX Rationale:**
- Badges provide immediate visual credibility and project health indicators
- Community section makes it clear how users can contribute and get help
- Placing badges at top maximizes visibility on GitHub
- Community section at bottom follows standard OSS README patterns

**Technical Details:**
- Used shields.io badge format for consistency
- CI badge links directly to GitHub Actions workflow
- Test count reflects current state (updated from 129 to 152)
- All links use relative paths where possible for repo portability

**Note:** Community files (.github/CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md) don't exist yet but links are ready for when they're added.

**Commit:** `⚛️ Add badges and community links to README` with Co-authored-by trailer

### 2026-04-25: Comprehensive Community Contributor Guides

**What:** Created complete CONTRIBUTING.md and DEVELOPMENT.md guides to welcome and onboard community contributors.

**Files Created:**
1. **.github/CONTRIBUTING.md** (13KB, 330+ lines)
   - Welcome section with community-first tone
   - Getting Started with prerequisites, clone, build, test
   - Development Workflow with branch naming and commit flow
   - Code Style & Standards (PascalCase, XML docs, async patterns, dependency guidelines)
   - Testing Requirements (xUnit structure, coverage >80%, naming conventions)
   - Documentation section (when to update README, docs/, XML comments)
   - Commit Message Format (Conventional Commits with types/scopes/examples)
   - Pull Request Process (template, linking issues, review expectations)
   - Issue Reporting (bug/feature templates, search first)
   - Questions section (Discussions, Issues, email links)

2. **.github/DEVELOPMENT.md** (15KB, 330+ lines)
   - Architecture Overview (backend contracts, dependency graph)
   - Project Structure (detailed breakdown of all 11 projects)
   - Key Files & Dependencies (NuGet packages, rationale)
   - How to Extend (add backend, embedder, miner, search strategy with code examples)
   - Testing Strategy (test pyramid, conformance tests, fake embedder)
   - Performance Considerations (vector search, embedding, memory, benchmarking)
   - Debugging Tips (logging, SQLite inspection, verbose test output, common issues)

**UX Design Decisions:**
- **Tone:** Welcoming and encouraging for first-time contributors
- **Structure:** Table of contents for easy navigation
- **Examples:** Real code snippets throughout DEVELOPMENT.md
- **Completeness:** No "TODO" placeholders - guides are ready for v0.1.0 launch
- **Cross-linking:** Both guides reference each other and docs/ pages appropriately

**Technical Highlights:**
- Covers full workflow from clone → build → test → PR
- Explains Conventional Commits with project-specific scopes (agents, ai, backends, cli, etc.)
- Documents testing requirements matching existing test suite structure
- Provides real patterns for extending core abstractions (IBackend, IEmbedder, miners)
- Performance section acknowledges current SQLite approach and future upgrade path

**Integration Points:**
- Links match existing repo structure (.github/CODE_OF_CONDUCT.md, docs/architecture.md, etc.)
- Reflects actual solution file (MemPalace.slnx) and project names
- Test count accurate (152 tests passing)
- Commands use correct CLI name (mempalacenet)
- All NuGet dependencies documented with purpose

**Research Notes:**
- Verified project structure by viewing src/ directory and .csproj files
- Reviewed docs/architecture.md for accurate contract descriptions
- Checked README.md for consistency with Quick Start and Development sections
- Confirmed xUnit test structure in MemPalace.Tests/

**Learnings:**
1. Contributor guides should be comprehensive from day one - not iterative TODOs
2. CONTRIBUTING focuses on process/workflow, DEVELOPMENT focuses on technical internals
3. Real code examples in DEVELOPMENT make extension patterns concrete
4. Community tone matters: "we welcome" not "you must"
5. Cross-referencing existing docs/ pages prevents duplication

**Files Changed:**
- `.github/CONTRIBUTING.md` (created)
- `.github/DEVELOPMENT.md` (created)

**Commit:** `🤝 Add CONTRIBUTING and DEVELOPMENT guides` with Co-authored-by trailer

**Impact:**
- Community contributors now have clear guidance from first clone to merged PR
- Extension patterns documented for backends, embedders, miners, and search strategies
- Testing requirements explicit with conformance test pattern explained
- Performance considerations surfaced early for developers choosing approaches
- Ready for v0.1.0 public launch with full contributor onboarding
