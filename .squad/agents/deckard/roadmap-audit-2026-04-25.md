# Roadmap Audit — 2026-04-25

**Deckard (Lead / Architect)**  
**Session:** Roadmap Completeness Review

---

## Executive Summary

MemPalace.NET is **architecturally complete through v0.1.0**, with all 10 phases designed and 9 of 10 substantively delivered. Build passes, 150 tests green. However, three categories of work remain:

1. **Process gaps** (decision review, CI workflow)
2. **Documentation discrepancies** (MCP tools count; wake-up command status)
3. **Forward-looking work** (expanded MCP surface, BM25, real datasets)

No blocking issues. Phase 9 (Bryant, benchmarks) is in progress with clean builds; Phase 10 (Deckard, v0.1 polish) is complete pending decision inbox review.

---

## Project State

### Phases Delivery Status

| Phase | Title | Owner | Status | Commit | Notes |
|-------|-------|-------|--------|--------|-------|
| 0 | Scaffold & CI | Deckard | ✅ Done | 2b8d2fc | All projects created, CI skeleton |
| 1 | Core Domain | Tyrell | ✅ Done | c0622dd | Backend contracts, conformance suite |
| 2 | SQLite Backend | Tyrell | ✅ Done | f9ea617 | BLOB vectors + cosine similarity |
| 3 | AI Integration | Roy | ✅ Done | a1a265f | M.E.AI + ONNX embedder (ElBruno.LocalEmbeddings) |
| 4 | Mining & Search | Tyrell+Roy | ✅ Done | 0b2bda2 | Filesystem, conversations, hybrid search, rerank |
| 5 | CLI | Rachael | ✅ Done | 93d4f96 | Spectre.Console.Cli, commands working |
| 6 | Knowledge Graph | Roy+Tyrell | ✅ Done | 6e9916d | Temporal triples, validity windows |
| 7 | MCP Server | Roy | ✅ Done | 7e213e5 | ModelContextProtocol 1.2.0, 7 tools (see gap) |
| 8 | Agent Framework | Roy | ✅ Done | 12dc957 | Microsoft.Agents.AI, per-agent diaries |
| 9 | Benchmarks | Bryant | 🚧 In progress | 6ae17f6 | LongMemEval harness, synthetic datasets, builds green |
| 10 | Polish & v0.1 | Deckard | ✅ Done | 93cd7f9 | NuGet metadata, README, CI pack job |

**Summary:** 8 complete ✅, 1 in progress 🚧, 1 complete ✅

### Build & Test Status

```
dotnet build src/ -c Release
→ ✅ Build succeeded (0 errors, 0 warnings)

dotnet test src/ -c Release --no-build
→ ✅ All 150 tests passing

dotnet pack src/ -c Release
→ ✅ Packages created (MemPalace.Core.0.1.0.nupkg verified)
```

No blockers. System is buildable and testable.

---

## Missing / Incomplete Work

### Category A: Process Gaps

#### 1. Decision Inbox Not Merged

**Status:** 5 decisions in `.squad/decisions/inbox/` pending review + merge to `.squad/decisions.md`:

- `roy-mcp.md` — Phase 7 MCP Server decision (tool surface, package choices)
- `roy-agent-framework.md` — Phase 8 Agent Framework initial attempt (removed, handrolled)
- `roy-agent-framework-real.md` — Phase 8 Agent Framework real integration (Microsoft.Agents.AI)
- `deckard-release-v0.1.md` — Phase 10 v0.1.0 release decisions (packaging, docs, CI)
- `bryant-benchmarks.md` — Phase 9 Benchmarks harness decisions (metrics, JSONL, synthetic fixtures)

**Action:** Deckard should move inbox → main decisions.md (formal archival + discoverability).

**Impact:** Low — code is committed, decisions are documented; this is bookkeeping.

---

#### 2. CI Workflow Runs Only on Tags

**Current behavior:**
```yaml
on:
  push:
    tags: ['v*']
  workflow_dispatch:
```

**Problem:** No validation on regular pushes to main or PRs. Build is only verified when a tag is created.

**Implication:** Regressions on main branch won't surface until tagging (too late for v0.1).

**Fix required:**
```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:
  
jobs:
  build:
    runs-on: ubuntu-latest
    # ... existing steps ...
    
  pack:
    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v')
    needs: build
    # ... existing steps ...
```

**Impact:** Medium — Good practice. Should be fixed before tagging v0.1.0 to catch issues during next main pushes.

---

### Category B: Documentation Discrepancies

#### 1. MCP Tool Count Mismatch

**Claim:** README.md, CHANGELOG.md, docs/PLAN.md all claim "29 tools"

**Actual:** Only 7 tools implemented:
- Palace: `palace_search`, `palace_recall`, `palace_get`, `palace_list_wings`, `palace_health` (5)
- Knowledge Graph: `kg_query`, `kg_timeline` (2)

**Intent:** The PLAN.md originally said "expand toward parity with Python's 29-tool surface" (post-v0.1 work). But README and CHANGELOG were written as if 29 tools are delivered.

**Fix required:**
- Update README.md: "MCP server (7 tools)" with note "Expand to 29-tool parity post-v0.1"
- Update CHANGELOG.md Phase 7 entry: "7 tools: palace read/write, KG ops"
- Keep PLAN.md unchanged (correctly documents forward path)

**Impact:** Low — Doesn't affect functionality, but misleads users about surface area. Should fix before publishing to NuGet.

---

#### 2. Wake-Up Command Undocumented as Incomplete

**Claim:** README quick start includes:
```bash
mempalacenet wake-up
```

**Actual:** CLI doesn't implement `wake-up` command.

**Status:** PLAN.md correctly lists as post-v0.1 ("load context summary for new session").

**Fix required:**
- Remove `wake-up` from README quick start (or note as "coming soon")
- Confirm `mempalace search`, `mempalace mine`, `mempalace agents run`, `mempalace kg query` all work

**Impact:** Low-medium — Users will hit missing command if following quick start literally.

---

### Category C: Forward-Looking Work (Post-v0.1, Not Blockers)

These are documented as roadmap items, not gaps:

#### 1. Expanded MCP Tool Surface
- Current: 7 tools (search, recall, get, list_wings, health, kg_query, kg_timeline)
- Target: 29 tools (includes write ops, agent diary access, advanced KG)
- Status: Deferred to Phase 11+

#### 2. BM25 Keyword Search
- Current: Token overlap
- Target: BM25 (probabilistic relevance)
- Status: Documented in PLAN.md as post-v0.1

#### 3. Real Dataset Integration
- Current: Synthetic 5-item JSONL fixtures for CI
- Target: Real LongMemEval, LoCoMo, ConvoMem datasets
- Status: Bryant (Phase 9) notes "Bruno will fetch separately"

#### 4. Vector Store Upgrade
- Current: O(n) brute-force cosine (acceptable <100K vectors)
- Target: sqlite-vec or Qdrant
- Status: PLAN.md lists as post-v0.1 option

---

## Architectural Completeness vs. README

| Feature | README Claims | Implemented | Gap |
|---------|---------------|-------------|-----|
| Local-first ONNX embeddings | ✅ | ✅ (ElBruno.LocalEmbeddings) | None |
| Microsoft.Extensions.AI | ✅ | ✅ (v10.5.0) | None |
| Temporal KG | ✅ | ✅ | None |
| SQLite backend | ✅ | ✅ | None |
| CLI commands | `init`, `mine`, `search`, `agents`, `mcp`, `kg` | 6/6 implemented | None |
| MCP server | 29 tools | 7 tools (71% gap) | **Doc issue** |
| Agent Framework | ✅ | ✅ (Microsoft.Agents.AI) | None |
| Wake-up command | ✅ (in quick start) | ❌ | **Missing** |

---

## Decisions Pending Formal Review

### From Inbox → Main Decisions File

1. **Roy (Phase 7):** MCP package v1.2.0 stable (not preview), ModelContextProtocol compatible
2. **Roy (Phase 8, v1):** Removed Microsoft.Agents.AI due to perceived conflict (later reversed)
3. **Roy (Phase 8, v2):** Restored real Microsoft.Agents.AI (stable, no conflicts, proper agent orchestration)
4. **Deckard (Phase 10):** NuGet metadata consolidated, README hardened, CI pack job added
5. **Bryant (Phase 9):** Benchmarks harness with IBenchmark interface, JSONL streaming, BenchmarkDotNet, NDCG scoring

**Action:** These decisions are correct and should be moved to `.squad/decisions.md` for historical record + team reference.

---

## Critical Path for v0.1.0 Release

### Must-Do (Blocking)
1. ✅ Phase 9 (Bryant): Build passes, tests pass (150 green), no code blockers
2. 🔧 **Fix CI workflow** to run on main pushes + PRs (not just tags)
3. 🔧 **Update README:** Remove/clarify 29 tools claim, check wake-up command
4. 🔧 **Move inbox decisions** to main file (optional but good practice)

### Nice-to-Have (Polish)
- Run benchmarks against real datasets (Bryant will handle)
- Validate R@5 parity (Phase 9 scope)
- Expand MCP tool surface (post-v0.1)

---

## Recommendations

### Immediate (Before Tag)

1. **Deckard:** Fix CI workflow to trigger on main pushes + PRs.
   - Rationale: Catch regressions before tag, follow CI best practices.
   - Effort: 5 min (YAML edit).
   - Priority: Medium.

2. **Deckard:** Test quick start commands end-to-end.
   - Rationale: Verify `mempalacenet init`, `mine`, `search`, `agents` work as documented.
   - Effort: 30 min (manual CLI testing).
   - Priority: Medium.

3. **Deckard:** Update README MCP tools count + clarify wake-up as future.
   - Rationale: Avoid misleading users.
   - Effort: 5 min (doc edit).
   - Priority: Medium.

4. **Deckard:** Move inbox decisions to main file.
   - Rationale: Complete handoff documentation.
   - Effort: Copy + cleanup.
   - Priority: Low (for record-keeping).

### Post-v0.1 (Roadmap)

1. **All:** BM25 keyword search (Phase 11).
2. **Roy+Tyrell:** Expand MCP surface to 29 tools (Phase 11).
3. **Bryant:** Real dataset integration + R@5 trending (Phase 9 follow-up).
4. **Tyrell:** Vector store upgrade to sqlite-vec or Qdrant (Phase 12).

---

## Team Workload Assessment

| Member | Phase(s) | Status | Blockers | Next Action |
|--------|----------|--------|----------|------------|
| **Deckard** | 0, 10 | ✅ Done | None (CI fix needed) | Fix CI + doc + decisions review |
| **Tyrell** | 1, 2, 4, 6 | ✅ Done | None | Standby (post-v0.1: Phase 11+) |
| **Roy** | 3, 6, 7, 8 | ✅ Done | None | Standby (post-v0.1: Phase 11+) |
| **Rachael** | 5 | ✅ Done | None | Standby (post-v0.1: Phase 11+) |
| **Bryant** | 9 | 🚧 In progress | None (builds green) | Complete benchmarking, merge to main |

---

## Conclusion

**MemPalace.NET v0.1.0 is ready for tagging**, contingent on:

1. ✅ All Phases 0-8 complete + tested
2. ✅ Phase 9 (benchmarks) in progress, builds passing
3. ✅ Phase 10 (polish) complete
4. 🔧 **3 small fixes** (CI workflow, docs, quick start validation)

**No architectural gaps. No missing core features.** The "missing jobs" are:
- Process formalization (inbox → decisions)
- Documentation accuracy (tool count, wake-up status)
- CI coverage (pushes, not just tags)

All fixable in <1 hour. Ready for Bruno to proceed with v0.1.0 tag once these items addressed.

---

**Prepared by:** Deckard  
**Date:** 2026-04-25  
**Next Review:** Post v0.1.0 release (plan Phase 11)
