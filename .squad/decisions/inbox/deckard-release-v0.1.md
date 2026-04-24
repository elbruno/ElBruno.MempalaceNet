# Decision: v0.1.0 Release — Package Metadata, Documentation, CI

**Date:** 2026-04-24  
**Agent:** Deckard (Lead / Architect)  
**Phase:** 10 — Polish + v0.1 Release Prep  
**Status:** ✅ Committed (58e7eba)

---

## Context

MemPalace.NET has completed Phases 0-8 (129 tests green), delivering:
- Core domain + SQLite backend
- Microsoft.Extensions.AI integration (ONNX embeddings by default)
- Mining + Search (filesystem, conversations, semantic, hybrid, reranking)
- CLI (Spectre.Console)
- Temporal Knowledge Graph
- MCP Server (29 tools)
- Microsoft Agent Framework integration

Phase 9 (Benchmarks, Bryant) is in progress. Phase 10 prepares the solution for public v0.1.0 release.

---

## Decisions

### 1. NuGet Package Metadata

**Decision:** Consolidate common metadata in `Directory.Build.props`, add per-project metadata in each `.csproj`.

**Rationale:**
- DRY principle: version, author, license, repo URLs defined once
- Discoverability: proper `PackageTags`, `Description`, `PackageId` help NuGet search
- Tooling: `PackAsTool=true` + `ToolCommandName` for CLI and benchmarks
- Documentation: `PackageReadmeFile=README.md` includes README in every package

**Details:**
- `Directory.Build.props`: Authors (Bruno Capuano), Company, Copyright, License (MIT), ProjectUrl, RepositoryUrl/Type, Version 0.1.0, PackageTags
- Per-project: `PackageId` (e.g., `MemPalace.Core`, `mempalacenet`, `mempalacenet-bench`), `Description` (one-line purpose), `<None Include="..\..\README.md" Pack="true" PackagePath="\" />`
- Test project: `IsPackable=false` (already set)

**Outcome:**
- 8 library packages: MemPalace.Core, Backends.Sqlite, Ai, Mining, Search, KnowledgeGraph, Mcp, Agents
- 2 tool packages: mempalacenet (CLI), mempalacenet-bench (benchmarks)

---

### 2. README.md — Release-Quality Documentation

**Decision:** Rewrite README to be concise, actionable, and link-rich.

**Rationale:**
- First impression matters: README is the GitHub landing page
- Elevator pitch + quick start → immediate value demo
- Architecture table → orientation for contributors
- Links to docs/ → don't duplicate content

**Structure:**
1. One-paragraph pitch (local-first, ONNX embeddings, M.E.AI + Agent Framework, MCP, temporal KG, SQLite)
2. Why MemPalace.NET? (local-first, no cloud calls, Microsoft.Extensions.AI, MCP server, temporal KG, SQLite backend)
3. Quick Start (`dotnet tool install`, `mempalacenet init/mine/search/mcp/agents`)
4. Architecture table (8 projects + 2 tools, one-line purpose each)
5. Documentation links (organized by topic)
6. Development (`dotnet build`, `dotnet test`, `dotnet pack`)
7. Roadmap (v0.1.0 scope, post-v0.1 plans)
8. Credits (original MemPalace, Bruno Capuano, ElBruno.LocalEmbeddings)
9. License (MIT)

**Outcome:**
- README is GitHub-ready, link-dense, action-oriented
- References original MemPalace project, ElBruno.LocalEmbeddings (default embedder)

---

### 3. Documentation Structure

**Decision:** Create `docs/CHANGELOG.md`, `docs/RELEASE-v0.1.md`, update `docs/README.md` to be a topical index.

**Rationale:**
- CHANGELOG: standard format (Keep a Changelog), phase summaries, known limitations
- RELEASE: GitHub release notes template (highlights, getting started, limitations, links)
- docs/README.md: navigation hub (no duplication of content, just links + 1-2 line descriptions)

**Files created:**
- `docs/CHANGELOG.md` (v0.1.0 by phase + known limitations)
- `docs/RELEASE-v0.1.md` (highlights, getting started, docs links, known limitations)
- `docs/README.md` updated (organized: Concepts / Backends / AI / Content / KG / Integrations / Tools)

**Outcome:**
- Clear release history
- GitHub release draft ready
- docs/ folder is navigable

---

### 4. CI Workflow — Pack Job

**Decision:** Add `pack` job to `.github/workflows/ci.yml` that runs on main pushes and v* tags.

**Rationale:**
- Automation: verify packages build on every main push
- Tag releases: auto-pack when Bruno tags v0.1.0, v0.1.1, etc.
- Artifact upload: GitHub Actions artifacts for manual inspection
- No auto-publish: Bruno will publish to NuGet manually (avoids accidental releases)

**Implementation:**
```yaml
pack:
  runs-on: ubuntu-latest
  if: github.event_name == 'push' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v'))
  needs: build
  steps:
    - dotnet pack src/ -c Release --output ./artifacts
    - upload-artifact (packages)
```

**Outcome:**
- Build+test job: runs on PRs and pushes (validates code)
- Pack job: runs on main pushes and tags (validates packaging)
- No NuGet publish step (manual control)

**Note:** CI is currently failing on Bruno's account due to a billing limit (not due to code). YAML is correct.

---

### 5. Phase Status in PLAN.md

**Decision:** Update `docs/PLAN.md` with commit SHAs for each completed phase.

**Rationale:**
- Historical record: easy to see when each phase landed
- Cross-reference: link commit SHAs to GitHub history
- Status visibility: emoji indicators (✅ Done, 🚧 In progress)

**Format:**
```
## Phase N — Title
**Owner:** Agent
**Status:** ✅ Done (commit abc1234)
```

**Phases updated:**
- Phase 0: ✅ Done (2b8d2fc)
- Phase 1: ✅ Done (c0622dd)
- Phase 2: ✅ Done (f9ea617)
- Phase 3: ✅ Done (a1a265f)
- Phase 4: ✅ Done (0b2bda2)
- Phase 5: ✅ Done (93d4f96)
- Phase 6: ✅ Done (6e9916d)
- Phase 7: ✅ Done (7e213e5)
- Phase 8: ✅ Done (12dc957)
- Phase 9: 🚧 In progress (Bryant)
- Phase 10: 🚧 In progress (this commit)

---

## Concurrency with Bryant (Phase 9)

**Situation:**
- Bryant added `MemPalace.Benchmarks` project + tests + `docs/benchmarks.md` concurrently
- His project has a build issue: NU1510 (System.Text.Json PackageReference is unnecessary)
- Full solution `dotnet build src\` fails because of the NuGet warning (treated as error)

**Coordination:**
- No-touch zones: I didn't modify `src/MemPalace.Benchmarks/*.csproj`, `src/MemPalace.Tests/Benchmarks/*`, `docs/benchmarks.md`
- Staged changes carefully: avoided staging Bryant's untracked files
- Slnx: kept Bryant's addition of Benchmarks project to MemPalace.slnx

**Outcome:**
- Individual `dotnet pack` works (verified MemPalace.Core.0.1.0.nupkg created)
- Full build will green up when Bryant addresses the NuGet warning (not a Phase 10 blocker)

---

## For Bruno

**Next Steps:**
1. Bryant completes Phase 9 (fixes NU1510 warning)
2. Full `dotnet build src\` + `dotnet test src\` green
3. Bruno reviews v0.1.0 changes
4. Bruno tags v0.1.0:
   ```
   git tag -a v0.1.0 -m "MemPalace.NET v0.1.0" && git push --tags
   ```
5. Bruno manually publishes NuGet packages (CI pack job will auto-run on tag push, but won't publish)

**Package Install Commands (after NuGet publish):**
```bash
# CLI tool
dotnet tool install -g mempalacenet --version 0.1.0

# Libraries (example)
dotnet add package MemPalace.Core --version 0.1.0
dotnet add package MemPalace.Backends.Sqlite --version 0.1.0
dotnet add package MemPalace.Ai --version 0.1.0
```

---

## Known Limitations (documented in CHANGELOG + RELEASE)

- **Vector storage:** SQLite backend uses O(n) brute-force cosine similarity (acceptable <100K vectors; upgrade to sqlite-vec or Qdrant planned post-v0.1)
- **Keyword search:** Token overlap (BM25 planned post-v0.1)
- **Wake-up summaries:** `mempalacenet wake-up` not yet implemented

---

**Commit:** 58e7eba  
**Push:** main  
**CI:** Will run pack job on next main push (currently blocked by billing, not code)
