# v0.7.0 Phase 2 & Phase 3 Roadmap

**Version:** v0.7.0  
**Lead Architect:** Deckard  
**Date:** 2026-04-27  
**Status:** Phase 1 COMPLETE ✅ → Phase 2 ACTIVE 🚀

---

## Executive Summary

**Phase 1 Completion Status:**
- ✅ **Tyrell:** MCP SSE transport layer (HttpSseTransport, SessionManager, unit tests) - [commit 1806192]
- ✅ **Roy:** Wake-up LLM summarization (IMemorySummarizer, WakeUpCommand functional) - [commit TBD]
- ✅ **Rachael:** Skill CLI Phase 1 (SkillManager, 6 CLI commands) - [commit TBD]

**v0.7.0 Theme:** *Agent Workflows & Integrations*

**Remaining effort:** 3-4 weeks (Phase 2: 1-2 weeks, Phase 3: 1-2 weeks)

---

## Phase 1 Accomplishments (Week 1)

### ✅ MCP SSE Transport (Tyrell)
**Delivered:**
- `IMcpTransport` abstraction for stdio/SSE coexistence
- `SessionManager` with crypto-secure 32-byte tokens, 60-min expiry
- `HttpSseTransport` with ASP.NET Core Minimal API (POST/GET/DELETE endpoints)
- 29 unit tests (100% SessionManager, 85% HttpSseTransport coverage)
- `docs/guides/mcp-sse-transport-setup.md` (10KB protocol guide)

**Architecture:**
```
POST /mcp    → client→server JSON-RPC (creates/validates session)
GET /mcp     → server→client SSE stream (text/event-stream)
DELETE /mcp  → session cleanup
```

**Security:** Localhost-only (127.0.0.1), origin validation, CSPRNG session tokens

**Status:** ✅ Core transport DONE. **Next:** CLI integration (`--transport sse`)

---

### ✅ Wake-Up LLM Summarization (Roy)
**Delivered:**
- `IMemorySummarizer` abstraction (`LLMMemorySummarizer`, `NoOpMemorySummarizer`)
- `WakeUpCommand` fully functional (was stub) with DI injection
- Config-driven DI: uses `IChatClient` if available, graceful degradation
- 7 unit tests for summarization layer (MockChatClient pattern)
- `docs/guides/wake-up-summarization.md` already exists

**Key features:**
- Cost control: 50 memories max, 512 token limit
- Graceful fallback: metadata-only display if LLM unavailable
- Spectre.Console: panels, spinners, rich output

**Status:** ✅ Summarization layer DONE. **Next:** Backend `WakeUpAsync()` optimization (Tyrell Phase 2)

---

### ✅ Skill Marketplace CLI (Rachael)
**Delivered:**
- `SkillManifest` model (Core) with JSON serialization
- `SkillManager` service (CLI) for local CRUD operations
- 6 CLI commands: `skill list`, `search`, `info`, `install`, `enable/disable`, `uninstall`
- Skills stored in `~/.palace/skills/` with manifest validation
- Tests: `SkillManagerTests`, `SkillManifestTests`

**Status:** ✅ Phase 1 (local filesystem) DONE. **Next:** MCP integration (Phase 2), remote registry (Phase 3)

---

## Phase 2: Integration & Optimization (Weeks 2-3)

**Goal:** Connect Phase 1 components, integrate with MCP server, optimize backend queries.

**Duration:** 1-2 weeks  
**Parallel workstreams:** 3 (CLI integration, MCP tools, backend optimization)

### 🔧 Workstream A: CLI Integration (Rachael + Tyrell)
**Owner:** Rachael (lead), Tyrell (support)  
**Effort:** 3-5 days

#### Tasks:
1. **MCP CLI integration** (`--transport sse`)
   - Update `McpCommand` to accept `--transport` flag (stdio | sse)
   - Wire `HttpSseTransport` when `--transport sse`
   - Add `--port` option (default 5050)
   - Example: `mempalacenet mcp --transport sse --port 5050`
   - **Tests:** CLI parse tests, transport selection logic
   - **Docs:** Update `docs/mcp.md` with SSE transport usage

2. **Skill CLI MCP integration**
   - Connect `SkillManager` to MCP tools (via `IMcpClient` wrapper)
   - Enable remote skill search (if MCP server exposes skill catalog)
   - **Deferred:** Remote registry install (Phase 3 if time permits)

3. **CLI UX polish** (Issue #7)
   - Progress bars for `mine`, `search --rerank`
   - Better error messages with remediation steps
   - `--verbose` flag for debugging output
   - EntityRef documentation in help text

**Success Criteria:**
- ✅ `mempalacenet mcp --transport sse` starts HTTP server on port 5050
- ✅ SSE clients can connect and receive tool updates
- ✅ `skill` commands work with local skills
- ✅ Progress bars display in long-running commands
- ✅ Error messages include actionable next steps

**Dependencies:** None (all Phase 1 work complete)

---

### 🤖 Workstream B: MCP Tool Expansion (Roy)
**Owner:** Roy  
**Effort:** 4-6 days

#### Tasks:
1. **Expand MCP toolset from 7 → 15 tools** (Issue #6)
   - **Read tools (existing 7):** palace_search, palace_recall, palace_get, palace_list_wings, kg_query, kg_timeline, palace_health
   - **New write tools (8):**
     - `palace_store` - Add new memory
     - `palace_update` - Update existing memory by ID
     - `palace_delete` - Delete memory by ID
     - `palace_batch_store` - Bulk add memories
     - `kg_add_entity` - Add knowledge graph entity
     - `kg_add_relationship` - Add KG triple
     - `palace_create_collection` - Create new collection
     - `palace_delete_collection` - Delete collection
   - **Error handling:** Proper validation, error messages
   - **Tests:** Unit tests for each new tool (≥80% coverage)
   - **Docs:** Update `docs/mcp.md` with tool catalog

2. **Agent diary integration** (if bandwidth)
   - Ensure agent diary works with MCP tools
   - Test agent→palace→MCP round-trip

**Success Criteria:**
- ✅ 15 tools total (7 read + 8 write)
- ✅ Write operations validated and tested
- ✅ Clear separation: read tools (safe), write tools (require confirmation)
- ✅ All tools work with Claude/Agent clients

**Dependencies:** None (MCP server already functional with 7 tools)

---

### 🗄️ Workstream C: Backend Query Optimization (Tyrell)
**Owner:** Tyrell  
**Effort:** 2-3 days

#### Tasks:
1. **Add `IBackend.WakeUpAsync()` method**
   - Server-side date filtering (vs current client-side)
   - SQL: `SELECT * FROM [collection] WHERE timestamp >= @startDate ORDER BY timestamp DESC LIMIT @limit`
   - Indexed timestamp column for performance
   - **Tests:** Backend conformance tests for WakeUpAsync
   - **Integration:** Update Roy's WakeUpCommand to use new method

2. **Query optimization review**
   - Profile vector search performance (brute-force cosine)
   - Identify slow queries (>100ms) in SQLite backend
   - Add indexes if needed (metadata filters, timestamp)
   - Document optimization opportunities for future

3. **Embedder interface prep** (if time, else Phase 3)
   - Review GitHub issue #43 (ElBruno.LocalEmbeddings API changes)
   - Plan `ICustomEmbedder` interface for factory pattern
   - **Deferred to Phase 3** if ElBruno.LocalEmbeddings not stable

**Success Criteria:**
- ✅ `WakeUpAsync()` implemented and tested
- ✅ Wake-up queries <50ms (10K memories)
- ✅ Indexed queries use SQL indexes correctly
- ✅ Profile report documents bottlenecks

**Dependencies:** Roy's Phase 1 wake-up work (for integration testing)

---

## Phase 3: Embedder Interface & Release Prep (Weeks 4-5)

**Goal:** Finalize embedder factory pattern, polish documentation, release v0.7.0.

**Duration:** 1-2 weeks  
**Parallel workstreams:** 2 (embedder interface, release prep)

### 🔧 Workstream D: Embedder Factory Pattern (Tyrell + Roy)
**Owner:** Tyrell (lead), Roy (review)  
**Effort:** 3-4 days

#### Tasks:
1. **Create `ICustomEmbedder` interface**
   - Abstraction for user-provided embedders
   - Methods: `EmbedAsync(string text)`, `GetDimensions()`
   - Property: `EmbedderIdentity` (for validation)
   - **Goal:** Allow users to plug in custom embedders without modifying MemPalace.Ai

2. **Implement embedder factory**
   - Factory pattern: `EmbedderFactory.Create(EmbedderOptions options)`
   - Built-in: `LocalEmbedder` (ElBruno.LocalEmbeddings), `OpenAIEmbedder` (M.E.AI.OpenAI)
   - Custom: User implements `ICustomEmbedder`, passes to factory
   - **DI registration:** `services.AddMemPalaceAi(options => options.CustomEmbedder = myEmbedder)`

3. **Update ElBruno.LocalEmbeddings dependency** (Issue #43 blocker)
   - Wait for stable GitHub issue #43 resolution
   - Update NuGet reference to latest stable version
   - Verify API compatibility
   - **Fallback:** If not stable by release, ship with current version + migration guide

4. **Tests & docs**
   - Unit tests: Factory creation, custom embedder validation
   - Integration tests: Custom embedder end-to-end
   - Update `docs/ai.md` with custom embedder guide

**Success Criteria:**
- ✅ `ICustomEmbedder` interface defined and documented
- ✅ Factory pattern implemented with 2 built-ins + custom support
- ✅ Users can plug in embedders without code changes
- ✅ ElBruno.LocalEmbeddings dependency stable (or migration plan documented)

**Dependencies:** GitHub issue #43 resolution (external blocker)

---

### 📋 Workstream E: Release Preparation (Deckard + Bryant)
**Owner:** Deckard (lead), Bryant (QA validation)  
**Effort:** 5-7 days

#### Tasks:
1. **Documentation updates** (Issue #9)
   - Update `docs/SKILL_PATTERNS.md` with wake-up examples
   - Document MCP write operation patterns
   - Add agent diary + wake-up integration examples
   - Update `docs/cli.md` with all new commands
   - **Owner:** Deckard

2. **Integration testing** (Issue #10)
   - E2E tests: MCP + agents workflows
   - Scenario: Agent queries → stores → recalls memory cycle
   - Scenario: Knowledge graph updates via MCP tools
   - Scenario: Wake-up summarization with agent context
   - **Owner:** Bryant

3. **R@5 regression tests in CI** (Issue #8)
   - Automate LongMemEval R@5 measurement in GitHub Actions
   - Baseline: 96.6% R@5 (v0.6.0)
   - Alert threshold: <96.0% (0.6% regression tolerance)
   - **Owner:** Bryant

4. **Release checklist** (Issue #11)
   - Update CHANGELOG.md with v0.7.0 features
   - Review and update all README files
   - Test NuGet package structure
   - Prepare release notes with upgrade guide
   - Create GitHub Release with v0.7.0 tag
   - Publish all 10 packages to NuGet.org
   - **Owner:** Deckard

**Success Criteria:**
- ✅ All integration tests pass (≥85% coverage)
- ✅ R@5 regression tests automated in CI
- ✅ Documentation complete and up-to-date
- ✅ NuGet packages published successfully
- ✅ GitHub Release created with v0.7.0 tag

**Dependencies:** All Phase 2 workstreams complete

---

## Dependency Graph

```
Phase 1 (COMPLETE) ✅
├── MCP SSE Transport (Tyrell) ✅
├── Wake-up LLM (Roy) ✅
└── Skill CLI Phase 1 (Rachael) ✅

Phase 2 (Weeks 2-3) 🚀
├── Workstream A: CLI Integration (Rachael + Tyrell)
│   ├── MCP CLI --transport sse ← depends on: SSE transport ✅
│   ├── Skill CLI MCP integration ← depends on: Skill CLI Phase 1 ✅
│   └── CLI UX polish (Issue #7) ← independent
│
├── Workstream B: MCP Tool Expansion (Roy)
│   ├── 8 new write tools ← depends on: MCP SSE transport ✅
│   └── Agent diary integration ← depends on: Wake-up LLM ✅
│
└── Workstream C: Backend Optimization (Tyrell)
    ├── WakeUpAsync() method ← depends on: Wake-up LLM ✅
    └── Query optimization ← independent

Phase 3 (Weeks 4-5)
├── Workstream D: Embedder Interface (Tyrell + Roy)
│   ├── ICustomEmbedder interface ← independent
│   ├── Factory pattern ← depends on: ICustomEmbedder
│   └── ElBruno.LocalEmbeddings update ← BLOCKED by GitHub #43
│
└── Workstream E: Release Prep (Deckard + Bryant)
    ├── Documentation updates ← depends on: All Phase 2 workstreams
    ├── Integration testing ← depends on: All Phase 2 workstreams
    ├── R@5 regression tests ← independent
    └── Release checklist ← depends on: All Phase 2 + Phase 3 complete
```

**Critical path:** Phase 2A → Phase 2C → Phase 3E (CLI integration → Backend optimization → Release)

**Parallelization opportunities:**
- Phase 2: 3 workstreams run in parallel (no cross-dependencies)
- Phase 3D (embedder interface) can start during Phase 2 if GitHub #43 resolves early

---

## Risk Assessment

### High Risk 🔴
1. **ElBruno.LocalEmbeddings API changes** (GitHub #43)
   - **Impact:** Blocks embedder interface implementation
   - **Mitigation:** Ship v0.7.0 with current version + migration guide if not stable
   - **Owner:** Tyrell (monitor GitHub #43, prepare fallback)

### Medium Risk 🟡
2. **MCP SSE transport adoption** (new protocol)
   - **Impact:** Client integration issues, debugging SSE streams
   - **Mitigation:** Comprehensive docs, example clients, fallback to stdio
   - **Owner:** Tyrell + Rachael (docs + CLI integration)

3. **Integration testing complexity**
   - **Impact:** E2E tests may reveal cross-module bugs
   - **Mitigation:** Incremental testing, start early in Phase 2
   - **Owner:** Bryant (test plan ready, run tests during Phase 2)

### Low Risk 🟢
4. **Scope creep** (adding features beyond v0.7.0 plan)
   - **Impact:** Delays release timeline
   - **Mitigation:** v0.7.0 decisions are LOCKED (per mission brief)
   - **Owner:** Deckard (enforce scope, reject new features)

---

## Team Assignments

| Agent | Phase 2 (Weeks 2-3) | Phase 3 (Weeks 4-5) | Total Effort |
|-------|---------------------|---------------------|--------------|
| **Rachael** | CLI Integration (5d) + Skill MCP (3d) + UX polish (2d) | Release docs support (1d) | 11 days |
| **Tyrell** | Backend optimization (3d) + CLI support (1d) | Embedder interface (4d) | 8 days |
| **Roy** | MCP tool expansion (6d) | Embedder review (1d) | 7 days |
| **Bryant** | Testing support (2d) | Integration tests (3d) + R@5 CI (2d) | 7 days |
| **Deckard** | Architecture review (2d) | Documentation (4d) + Release prep (3d) | 9 days |
| **Total** | ~18 eng-days | ~18 eng-days | **36 eng-days** |

**Timeline:** 3-4 weeks (assuming 5-day work weeks, some parallelization)

---

## Success Metrics

### Phase 2 Exit Criteria
- ✅ `mempalacenet mcp --transport sse` works on localhost:5050
- ✅ 15 MCP tools implemented (7 read + 8 write)
- ✅ `WakeUpAsync()` backend method functional
- ✅ Progress bars in CLI (mine, search --rerank)
- ✅ All Phase 2 tests pass (≥85% coverage)

### Phase 3 Exit Criteria
- ✅ `ICustomEmbedder` interface documented
- ✅ Embedder factory pattern working (built-ins + custom)
- ✅ Integration tests pass (MCP + agents E2E)
- ✅ R@5 regression tests automated in CI
- ✅ Documentation complete (SKILL_PATTERNS, cli.md, ai.md)

### v0.7.0 Release Criteria
- ✅ All GitHub issues closed (except Ollama #4 - blocked by M.E.AI.Ollama stable)
- ✅ NuGet packages published (10 packages at v0.7.0)
- ✅ GitHub Release created with changelog
- ✅ README badge shows v0.7.0
- ✅ Zero P0/P1 bugs in issue tracker

---

## Deferred to v0.8.0

The following items are **explicitly out of scope** for v0.7.0:

1. **Remote skill registry** (Rachael)
   - Skill search/install from public registry
   - Skill dependency resolution
   - Skill versioning and updates
   - **Reason:** Phase 1 local-only sufficient for MVP

2. **Ollama embedder support** (Roy, Issue #4)
   - Blocked by Microsoft.Extensions.AI.Ollama stable release
   - **Reason:** ElBruno.LocalEmbeddings provides local-first alternative

3. **Advanced MCP features**
   - Multi-client session management
   - WebSocket transport
   - MCP protocol v2.0 features
   - **Reason:** SSE transport MVP sufficient for v0.7.0 use cases

4. **Performance optimization beyond query indexing**
   - Quantization (int8/bit vectors)
   - Approximate nearest neighbor (ANN) algorithms
   - Distributed backend support
   - **Reason:** Current performance acceptable for <100K memories

---

## Communication Plan

### Weekly Sync (Mondays, 10am)
- Progress review (each agent reports status)
- Blockers discussion
- Scope validation (reject new features)
- **Attendees:** All agents + Bruno

### Daily Standups (async via .squad/inbox.md)
- What did I complete yesterday?
- What am I working on today?
- Any blockers?

### Phase Transitions
- **Phase 1 → Phase 2:** This roadmap (2026-04-27)
- **Phase 2 → Phase 3:** Checkpoint meeting (2026-05-08 est.)
- **Phase 3 → Release:** Go/no-go decision (2026-05-15 est.)

---

## Open Questions for Bruno

1. **ElBruno.LocalEmbeddings GitHub #43:** What's the timeline for API stability? Should we ship v0.7.0 with current version + migration guide?

2. **MCP SSE default transport:** Should v0.7.0 keep stdio as default (backward compat) or switch to SSE (web-first)?

3. **Skill marketplace scope:** Is Phase 2 MCP integration sufficient, or should we prioritize remote registry (v0.8.0)?

4. **Release date target:** Is 2026-05-20 a hard deadline, or can we extend if GitHub #43 blocks embedder interface?

---

## Appendix: GitHub Issues Mapping

### Phase 1 (Complete) ✅
| Issue # | Title | Owner | Status |
|---------|-------|-------|--------|
| #2 | wake-up: Summarize recent memories | Roy | ✅ DONE |
| #3 | Fix CLI agents list DI bug | Rachael | ✅ DONE |
| #5 | MCP SSE transport support | Tyrell | ✅ DONE |

### Phase 2 (Active) 🚀
| Issue # | Title | Workstream | Owner | Status |
|---------|-------|-----------|-------|--------|
| #12 | MCP CLI --transport sse integration | A (CLI Integration) | Rachael + Tyrell | 🚀 ACTIVE |
| #13 | Skill CLI MCP integration | A (CLI Integration) | Rachael | 🚀 ACTIVE |
| #17 | CLI error messages with remediation steps | A (CLI Integration) | Rachael | 🚀 ACTIVE |
| #20 | Progress bars for long-running CLI commands | A (CLI Integration) | Rachael | 🚀 ACTIVE |
| #6 | MCP tool expansion (7 to 15 tools) | B (MCP Tools) | Roy | 🚀 ACTIVE |
| #14 | MCP write operations testing | B (MCP Tools) | Roy | 🚀 ACTIVE |
| #21 | MCP tool security validation (write operations) | B (MCP Tools) | Roy | 🚀 ACTIVE |
| #16 | Backend query optimization (WakeUpAsync) | B (Backend) | Tyrell | 🚀 ACTIVE |
| #15 | E2E test scenarios (MCP + Skills + Palace) | C (Integration Tests) | Bryant | 🚀 ACTIVE |
| #19 | CI/CD integration test workflow | C (Integration Tests) | Bryant | 🚀 ACTIVE |
| #18 | MCP tool catalog documentation | C (Documentation) | Deckard | 🚀 ACTIVE |
| #7 | CLI UX polish | A (CLI Integration) | Rachael | 🚀 ACTIVE |

### Phase 3 (Pending) ⏳
| Issue # | Title | Owner | Status |
|---------|-------|-------|--------|
| #8 | R@5 regression tests in CI | Bryant | ⏳ PENDING |
| #9 | Skill pattern documentation update | Deckard | ⏳ PENDING |
| #10 | Integration test coverage (master issue) | Bryant | ⏳ PENDING |
| #11 | v0.7.0 Release prep | Deckard | ⏳ PENDING |

### Deferred to v0.8.0
| Issue # | Title | Owner | Status |
|---------|-------|-------|--------|
| #4 | Restore Ollama support | Roy | ❌ BLOCKED |

**Legend:**
- ✅ DONE: Completed and committed
- 🚀 ACTIVE: In progress (Phase 2)
- ⏳ PENDING: Not started (Phase 3)
- ❌ BLOCKED: External dependency

---

## Change Log

| Date | Author | Change |
|------|--------|--------|
| 2026-04-27 | Deckard | Initial Phase 2-3 roadmap created |
| 2026-04-27 | Deckard | Phase 2 kickoff: 10 GitHub issues filed (#12-#21), workstreams assigned |

---

**Next Review:** 2026-05-08 (Phase 2 → Phase 3 transition)  
**Release Target:** 2026-05-20 (v0.7.0 public launch)  
**Phase 2 Status:** ACTIVE 🚀 (3 parallel workstreams)
