# Integration Test Strategy

**Author:** Bryant (Phase 2C)  
**Date:** 2025-01-15  
**Status:** Active

---

## Overview

This document outlines MemPalace.NET's integration test strategy for Phase 2C (v0.7.0). Integration tests validate end-to-end system behavior, focusing on **production backend performance** (SQLite) and **cross-component interactions**.

**Key Principle:** Integration tests measure **real-world latency** using SQLite backend + in-memory embeddings (FakeEmbedder). They complement unit tests (BackendConformanceTests) by validating performance targets.

---

## Test Layers

### 1. Unit Tests
- **Location:** `src/MemPalace.Tests/Backends/BackendConformanceTests.cs`
- **Scope:** Backend interface conformance (IBackend, ICollection)
- **Execution:** Both InMemory and SQLite backends
- **Purpose:** Verify functional correctness (CRUD operations, error handling)

### 2. Integration Tests (Phase 2C)
- **Location:** `src/MemPalace.Tests/Integration/`
- **Scope:** Production backend (SQLite) performance benchmarks
- **Execution:** SQLite backend only
- **Purpose:** Validate latency SLOs under realistic workloads

---

## Phase 2C Integration Tests

### WakeUpLatencyTests
- **Target:** `<50ms` semantic search latency for 1000 memories
- **Tolerance:** 20% (60ms max)
- **Scenarios:**
  1. Pure semantic search (QueryAsync)
  2. Hybrid search (semantic + metadata filter)
  3. Recency-only query (GetAsync with timestamp sort)

**Rationale:** WakeUp is the most latency-sensitive operation. Agent responsiveness depends on fast context retrieval.

### BranchCacheTests
- **Target:** `<1ms` for cached branch lookups (wing/room filters)
- **Tolerance:** 2ms (due to SQLite I/O variance)
- **Scenarios:**
  1. Wing filter (first hit baseline)
  2. Wing filter (second hit, cached)
  3. Room filter (cached)
  4. Multi-level filter (wing + room)

**Rationale:** Branch navigation is a core UX pattern. Caching ensures snappy hierarchical browsing.

### DeleteFilterTests
- **Target:** No strict latency (baseline tracking)
- **Scenarios:**
  1. Batch delete by IDs (50 records)
  2. Filtered delete (250 records)
  3. Delete + query consistency
  4. Filtered Get (metadata)
  5. Combined workflow (filter → delete → verify)

**Rationale:** Deletion throughput matters for memory cleanup. Filtered deletes enable time-based expiration.

---

## Test Execution

### CI/CD Pipeline
- **Workflow:** `.github/workflows/integration-tests.yml`
- **Triggers:**
  - Push to `main`
  - Pull requests
  - Nightly cron (2 AM UTC)
  - Manual dispatch

### Performance Metrics
- **Logging:** `Console.WriteLine("[PERF] ...")` captures metrics in CI logs
- **Baseline Tracking:** Nightly runs archive performance baselines (90-day retention)
- **Regression Detection:** Developers compare baselines before/after changes

### Test Data
- **WakeUpLatencyTests:** 1000 memories (production-scale dataset)
- **BranchCacheTests:** 100 memories (hierarchical metadata)
- **DeleteFilterTests:** 500 memories (batch operations)

---

## E2E Tests: Deferred to Phase 3

**Decision:** ADR-002 defers CLI/MCP E2E tests to Phase 3.

**Rationale:**
1. Phase 2 focused on core library stability (backends, search, KG)
2. CLI/MCP are thin wrappers over stable APIs
3. Integration tests provide sufficient coverage for backend SLOs
4. E2E infrastructure (process management, SSE mocking) is Phase 3 work

**Phase 3 Scope:**
- CLI command integration tests (init, mine, search, wake-up)
- MCP server SSE transport tests
- Agent diary workflow tests
- Cross-process interaction tests

---

## Running Tests Locally

### All tests
```bash
dotnet test src/MemPalace.Tests/MemPalace.Tests.csproj
```

### Integration tests only
```bash
dotnet test src/MemPalace.Tests/MemPalace.Tests.csproj \
  --filter "FullyQualifiedName~MemPalace.Tests.Integration"
```

### Specific test class
```bash
dotnet test src/MemPalace.Tests/MemPalace.Tests.csproj \
  --filter "FullyQualifiedName~WakeUpLatencyTests"
```

### With detailed performance output
```bash
dotnet test src/MemPalace.Tests/MemPalace.Tests.csproj \
  --filter "FullyQualifiedName~MemPalace.Tests.Integration" \
  --logger "console;verbosity=detailed"
```

---

## Interpreting Results

### Success Criteria
- ✅ All tests pass
- ✅ Performance metrics logged to console
- ✅ Latency within tolerance (60ms for WakeUp, 2ms for BranchCache)

### Failure Investigation
1. Check `[PERF]` logs for actual vs. target latency
2. Re-run locally to isolate CI environment factors
3. Compare against baseline (nightly artifacts)
4. Review SQLite query plans (if systematic regression)

### Tolerance Rationale
- **20% tolerance:** Accounts for CI environment variance (shared runners, cold caches)
- **No flake tolerance:** Performance tests should be deterministic after warmup
- **Baseline drift:** Track long-term trends via nightly runs

---

## Future Improvements (Phase 3+)

1. **Structured metrics:** Export JSON for automated regression detection
2. **Benchmark suite:** BenchmarkDotNet for microbenchmarking
3. **Stress tests:** 10k+ memory datasets (optional stretch goal)
4. **Distributed scenarios:** Multi-palace, cross-collection queries
5. **E2E automation:** Playwright/Spectre.Console testing for CLI

---

## References

- [Phase 2C Roadmap](./v070-phase2-phase3-roadmap.md)
- [Test Strategy (Phase 2)](./v070-test-strategy.md)
- [ADR-002: E2E Tests Deferred](../decisions/ADR-002-e2e-tests-deferred.md)
- [CI Workflow](../../.github/workflows/integration-tests.yml)

---

## Questions?

Slack: `#mempalace-dev` | GitHub Discussions | Copilot Skill: `mempalacenet`
