# ADR-002: E2E Tests Deferred to Phase 3

**Status:** Accepted  
**Date:** 2025-01-15  
**Author:** Bryant (Phase 2C)  
**Context:** v0.7.0 Phase 2C integration test implementation

---

## Context

Phase 2 delivered core library stability:
- ✅ SQLite backend (production-ready)
- ✅ In-memory backend (testing/prototyping)
- ✅ Semantic search (ONNX embeddings)
- ✅ Knowledge graph (temporal relationships)
- ✅ CLI tool (init, mine, search, wake-up, KG ops)
- ✅ MCP server (SSE transport for GitHub Copilot)

Phase 2C added **integration tests** to validate backend SLOs:
- WakeUpLatencyTests (<50ms semantic search)
- BranchCacheTests (<1ms cached lookups)
- DeleteFilterTests (baseline tracking)

**Question:** Should Phase 2C include CLI/MCP end-to-end (E2E) tests?

---

## Decision

**Defer CLI/MCP E2E tests to Phase 3.**

Phase 2C integration tests focus on **backend performance** (SQLite + FakeEmbedder). CLI/MCP E2E tests require additional infrastructure (process management, SSE mocking, Spectre.Console automation) that is out of scope for Phase 2.

---

## Rationale

### 1. Backend SLOs Are Critical Path
- **Backend latency** directly impacts agent responsiveness.
- Integration tests validate production backend (SQLite) performance.
- CLI/MCP are thin wrappers over backend APIs—functional correctness is verified by backend conformance tests.

### 2. CLI/MCP Are Stable
- CLI commands (`init`, `mine`, `search`, `wake-up`, `kg`) are production-ready.
- MCP server SSE transport is tested manually via GitHub Copilot integration.
- No known bugs or performance issues in CLI/MCP layer.

### 3. E2E Infrastructure Is Phase 3 Work
- **CLI E2E:** Requires process spawning, stdout/stderr capture, Spectre.Console mocking.
- **MCP E2E:** Requires SSE transport mocking, multi-turn conversation simulation.
- **Agent E2E:** Requires full workflow testing (mining → search → diary → KG).

Phase 3 will introduce:
- Playwright-style CLI automation
- SSE transport test harness
- Agent diary workflow tests

### 4. Integration Tests Provide Sufficient Coverage
- Backend conformance tests verify functional correctness.
- Integration tests validate latency SLOs (the main risk).
- CLI/MCP bugs are caught via:
  - Manual testing (developers use CLI daily)
  - GitHub Copilot dogfooding (MCP server in production)
  - Community feedback (v0.7.0 release)

---

## Consequences

### Benefits
✅ Faster Phase 2C delivery (3-4 days vs. 2 weeks)  
✅ Backend performance validated (highest risk area)  
✅ Clear Phase 3 scope (E2E infrastructure buildout)  
✅ No duplication of effort (CLI/MCP tests in Phase 3)

### Risks
⚠️ CLI/MCP bugs may slip into production  
**Mitigation:** Manual testing + GitHub Copilot dogfooding + rapid patch releases

⚠️ Regression risk for CLI commands  
**Mitigation:** Integration tests cover backend (90% of logic). CLI is a thin wrapper.

⚠️ No automated MCP transport testing  
**Mitigation:** Manual testing via GitHub Copilot. SSE protocol is stable (stdio fallback available).

---

## Alternatives Considered

### Alternative 1: Add CLI E2E Tests in Phase 2C
- **Pros:** Full test coverage, no deferred work
- **Cons:** 2-week delay, infrastructure complexity, limited ROI (CLI is stable)
- **Rejected:** Phase 2C scope is backend performance, not CLI automation.

### Alternative 2: Add Minimal "Smoke Tests" for CLI
- **Pros:** Quick sanity checks (e.g., `mempalacenet --version`)
- **Cons:** Low signal (doesn't test real workflows), still requires process management
- **Rejected:** Manual testing is more effective for smoke checks.

### Alternative 3: Defer All Integration Tests to Phase 3
- **Pros:** Minimal Phase 2C scope
- **Cons:** No performance baseline, backend SLO risk unmitigated
- **Rejected:** Backend performance is critical for v0.7.0 launch.

---

## Implementation

### Phase 2C (Accepted)
- ✅ WakeUpLatencyTests (SQLite backend, 1000 memories)
- ✅ BranchCacheTests (cached branch lookups)
- ✅ DeleteFilterTests (deletion throughput)
- ✅ GitHub Actions workflow (nightly baseline tracking)
- ✅ Integration test strategy guide

### Phase 3 (Deferred)
- ⏳ CLI command E2E tests (init, mine, search, wake-up, kg)
- ⏳ MCP server SSE transport tests
- ⏳ Agent diary workflow tests (mining → search → store → retrieve)
- ⏳ Cross-process integration tests (CLI ↔ MCP ↔ backend)
- ⏳ Stress tests (10k+ memories, optional)

---

## Success Metrics

### Phase 2C
- Integration tests run in CI (push, PR, nightly)
- Performance baselines archived (90-day retention)
- Zero test flakes (deterministic after warmup)

### Phase 3
- CLI E2E coverage >80% (all commands tested)
- MCP E2E tests validate SSE transport + multi-turn conversations
- Agent workflow tests validate end-to-end diary usage

---

## References

- [Integration Test Strategy](../guides/integration-test-strategy.md)
- [Phase 2C Roadmap](../guides/v070-phase2-phase3-roadmap.md)
- [Test Strategy (Phase 2)](../guides/v070-test-strategy.md)
- [GitHub Actions Workflow](../../.github/workflows/integration-tests.yml)

---

## Review Notes

- **Deckard (2025-01-15):** Approved. Backend SLOs are the priority. CLI/MCP E2E can wait.
- **Bryant (2025-01-15):** Implemented. Phase 2C delivers integration tests + workflow in 3-4 days.

---

## Changelog

| Date       | Author | Change |
|------------|--------|--------|
| 2025-01-15 | Bryant | Initial ADR created (Phase 2C integration test implementation) |
