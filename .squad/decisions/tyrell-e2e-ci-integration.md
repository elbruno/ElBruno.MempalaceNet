# Decision: E2E/Integration Test CI/CD Pipeline

**Agent:** Tyrell (Core Engine Dev)  
**Date:** 2026-04-30  
**Context:** Phase 2 Extension — E2E Tests  
**Status:** ✅ IMPLEMENTED

---

## Decision

Implemented GitHub Actions workflow (`.github/workflows/e2e-tests.yml`) to automatically run integration and E2E tests on every push and pull request.

---

## Problem Statement

Bryant (QA Lead) requires automated E2E test execution in CI/CD to:
1. Validate Palace API workflows on every change
2. Catch regressions in memory lifecycle, search quality, and performance
3. Report results to PR reviewers automatically
4. Support deterministic, repeatable test runs in CI environments

Pre-existing workflows (ci.yml, integration-tests.yml, regression-tests.yml) existed but had gaps:
- `ci.yml` only runs on tag pushes (not on PR/push to develop)
- `integration-tests.yml` runs unit tests with coverage, not full E2E workflows
- `regression-tests.yml` runs expensive benchmarks (LongMemEval R@5)

---

## Solution

### Workflow Design (`.github/workflows/e2e-tests.yml`)

**Trigger:**
- Push to `main` or `develop` (excluding doc-only changes)
- Pull requests targeting `main` or `develop`
- Manual trigger (`workflow_dispatch`)

**Steps:**
1. **Checkout** — Standard Git checkout (actions/checkout@v4)
2. **Setup .NET** — .NET 10.0 preview (matching ci.yml)
3. **Restore** — dotnet restore for dependency management
4. **Build** — dotnet build (Release mode, no restore needed with --no-restore)
5. **Run Tests** — dotnet test on MemPalace.Tests + MemPalace.E2E.Tests projects
   - Uses `--no-build` to skip rebuild (performance)
   - Generates TRX results for artifact upload
   - Verbose output for debugging
6. **Upload Artifacts** — Test results for 30 days (useful for post-mortem analysis)
7. **Parse Results** — Extract test counts from TRX XML
8. **Comment PR** — Posts summary to PR if applicable
9. **Fail on Error** — Explicit failure step for clarity

**Execution:**
- **OS:** ubuntu-latest (consistent with other workflows)
- **Timeout:** 15 minutes (generous for determinism, covers slow tests)
- **No caching:** Tests need clean state; caching adds complexity for marginal gain

### Artifact Strategy

- **Test Results:** Uploaded as `e2e-test-results` artifact (30-day retention)
- **PR Comments:** Parsed from TRX, includes pass/fail count
- **Failure Handling:** Explicit exit code propagation ensures CI shows ❌ on failure

---

## Key Design Decisions

### 1. **Single Workflow, No Duplication**

Initially considered separate E2E workflow + integration workflow. **Decision:** Consolidated into single workflow that runs all tests in both MemPalace.Tests and MemPalace.E2E.Tests projects.

**Rationale:**
- E2E tests exist in dedicated project (src/MemPalace.E2E.Tests/)
- Existing integration tests (MemPalace.Tests/Integration/) are functional
- Single workflow is simpler to maintain
- Avoid test duplication across workflows

### 2. **Release Configuration**

Use `-c Release` for all steps (build + test).

**Rationale:**
- Matches existing workflows' Release mode
- Tests should validate optimized code, not debug builds
- Performance tests (WakeUpLatency, BranchCache) require Release to be meaningful

### 3. **No Path Filters on Test Step**

Workflow triggers skip doc changes (via paths-ignore), but test step runs unconditionally.

**Rationale:**
- Tests are fast relative to full build (~10-30s for unit tests, excluding benchmarks)
- Avoids false negatives (e.g., a code change that looks like docs but affects tests)
- Simplifies logic

### 4. **TRX Results Format**

Use `--logger "trx"` + XML parsing instead of JSON.

**Rationale:**
- xUnit default; built-in with no extra tool installation
- GitHub Actions tooling supports TRX parsing
- Human-readable for debugging

### 5. **Run All Tests (No Conditional Filtering)**

All tests in both E2E and unit test projects run on every trigger.

**Rationale:**
- E2E tests and integration tests are tightly coupled
- Regression tests (benchmarks) should run to catch performance regressions
- Future: Can use [Trait] markers if separate fast CI track is needed

---

## Current State vs. Future State

### Current (This Implementation)
- ✅ Workflow runs all tests in MemPalace.Tests + MemPalace.E2E.Tests projects
- ✅ Reports pass/fail counts to PR
- ✅ Artifacts uploaded for diagnostics
- ✅ YAML syntactically valid; no linting errors
- ✅ E2E tests now in dedicated project (src/MemPalace.E2E.Tests/)
- ⚠️ Runs all tests including expensive benchmarks on every PR

### Future (Phase 2 Completion by Bryant)
- 📋 E2E tests fully refined and all passing
- 📋 Optional: Separate "fast" workflow (unit tests only) for PRs, full workflow for releases
- 📋 Performance optimizations: parallel test execution, conditional benchmark runs

---

## Implementation Notes

### Test Project Structure
- **Main Tests:** `src/MemPalace.Tests/`
- **E2E Tests:** `src/MemPalace.E2E.Tests/` (dedicated project)
- **Subfolders in MemPalace.Tests:** Ai, Agents, Backends, Cli, Diagnostics, Integration, KnowledgeGraph, Mcp, Mining, Search, Services

### Build & Test Flow
1. `.NET restore` — Pulls NuGet + local tool dependencies
2. `dotnet build --no-restore -c Release` — Compiles to Release (no restore, reuse from step 1)
3. `dotnet test --no-build -c Release` — Runs tests using pre-built binaries (fast, deterministic)

**Performance:** ~2-5 min for unit tests, ~8-12 min if regression tests run to completion.

---

## Rollback / Contingency

**If workflow causes CI to consistently fail:**
1. Comment out test step temporarily
2. Push with `[skip ci]` in commit message to bypass workflow
3. Investigate failing test; roll back if pre-existing issue
4. Update workflow and re-enable

**Pre-Existing Test Failures (Known):**
- 8 failing tests in base suite (SkillManagerTests x7, CommandAppParseTests x1)
- Not blocking Phase 2; part of ongoing Phase 1 refactoring

---

## Performance Impact

### CI Time Budget
- **Existing workflows:** 
  - ci.yml (tag push): ~5 min
  - integration-tests.yml (branch push): ~10-12 min (coverage collection)
  - regression-tests.yml (dispatch): ~8 min (benchmark)
- **New e2e-tests.yml (branch push/PR):** ~10-15 min (all tests)

### Recommendation
If CI time becomes critical, consider:
1. Splitting into "fast" (unit) and "slow" (integration + regression) workflows
2. Running regression tests only on `main` merges, not every PR
3. Using test matrix for parallelization (xUnit supports this)

---

## Cross-Team Coordination

### Bryant (QA Lead)
- E2E tests in dedicated project (src/MemPalace.E2E.Tests/)
- Workflow ready to execute tests once project is fully refined
- No action needed; workflow is infrastructure

### Roy (DevOps / Deployment)
- No new secrets or external services required
- Artifacts uploaded to GitHub Actions (standard)
- No changes to deployment pipeline

### Deckard (Docs / Release Mgmt)
- Workflow aligned with existing CI/CD practices
- No doc changes required at this stage
- Future: May want to link test results to release notes

---

## Testing & Verification

**Workflow Validation:**
- ✅ YAML syntax valid (no yamllint errors)
- ✅ Checked against GitHub Actions schema
- ⚠️ Full end-to-end test pending (requires merge to main/develop or manual dispatch)

**Local Test Command (for development):**
```bash
dotnet build src/ -c Release
dotnet test src/MemPalace.Tests/ -c Release --logger "console" -v normal
dotnet test src/MemPalace.E2E.Tests/ -c Release --logger "console" -v normal
```

---

## Maintenance & Future Improvements

### Low-Priority Tasks (Not Blocking)
1. Add test categories ([Trait] markers) for filtering
2. Create "fast" workflow variant for unit tests only
3. Parallelize tests across multiple jobs (xUnit/NUnit parallel mode)
4. Add code coverage badge to README
5. Generate HTML test report and link from PR

### Metrics to Monitor
- CI execution time per workflow
- Flaky test rate (tests that fail intermittently)
- PR feedback latency (time from push to CI result)

---

## Summary

✅ **Implemented:** E2E/Integration test workflow  
✅ **Status:** Ready for Phase 2 extension by Bryant  
✅ **Infrastructure:** Complete; test implementation ongoing  
📋 **Next:** Bryant to complete E2E test refinement and verify workflow execution  

---

## Decisions Log

| Date | Decision | Outcome |
|------|----------|---------|
| 2026-04-30 | Single consolidated workflow vs. separate E2E + integration | Consolidated; simpler maintenance, no duplication |
| 2026-04-30 | Run all tests vs. filtered tests | All tests; safer, easier to expand later |
| 2026-04-30 | TRX vs. JSON test results | TRX; built-in, GitHub Actions support |

---

**Approver:** Tyrell (Core Engine Dev)  
**Status:** ✅ APPROVED for Phase 2 workstream  
**Ready for:** Bryant to complete E2E test implementation  
