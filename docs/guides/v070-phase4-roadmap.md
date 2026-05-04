# v0.15.0 Phase 4 Roadmap

**Version:** v0.15.0  
**Lead Architect:** Deckard  
**Date:** 2026-05-04  
**Status:** Phase 4 KICKOFF 🚀

---

## Executive Summary

**Previous Phases Complete ✅:**
- Phase 2: CLI integration, MCP tool expansion, backend optimization (v0.7.0 shipped)
- Phase 3: Embedder interface, comprehensive testing, v0.15.0 released

**v0.15.0 Theme:** *Advanced E2E Journeys & Next-Gen Features*

**Phase 4 Focus:** Validate Phase 3 deliverables through advanced E2E testing; unblock Phase 5 (remote skill registry, Ollama, WebSocket).

**Remaining effort:** 2-3 weeks (Phase 4A: 1 week, Phase 4B-4C: 1-2 weeks)

---

## Phase 4 Accomplishments (v0.15.0 Feature Validation)

### Overview

Phase 4 extends the Phase 2 Extension E2E test suite with **3 advanced journey tests** that validate:
1. **LLM Reranking Journey** — Real-world quality improvement for search-first patterns
2. **Multi-Agent Memory Continuity** — Long-lived agent workflows with persistent state
3. **Full RAG Pipeline** — Complete semantic retrieval → injection → response cycle

These tests serve dual purposes:
- **Validation:** Confirm Phase 3D (embedder swapping) and Phase 3E (testing mandate) work in realistic scenarios
- **Documentation:** Provide executable examples of "experience library" patterns for developers

---

## Phase 4 Workstreams

### 🧪 Workstream A: E2E Advanced Tests (1 week)

**Owner:** Bryant (Tester)  
**Effort:** 5-7 days

#### Tasks:

1. **Reranking Journey Test** (~80-100 lines)
   - Initialize palace with 20 diverse documents
   - Semantic search + BM25 hybrid retrieval
   - Apply LLM-based reranker (Claude mini-model)
   - Verify top-1 result is definitively better than baseline (≥10% score improvement)
   - **Acceptance:** Test passes, scores measurable, latency <200ms
   - **Files:** `src/MemPalace.E2E.Tests/RerankingJourneyTests.cs`

2. **Multi-Agent Memory Continuity Test** (~100-120 lines)
   - Create agent diary wing ("agents/scribe")
   - Turn 1: Agent stores task context ("implementing auth")
   - Turn 2: Agent stores progress ("completed JWT validation")
   - Agent searches own diary for "auth" context
   - Verify both memories retrieved AND context injected into mock prompt
   - Verify coherence (no contradictions, agent recognizes continuity)
   - **Acceptance:** 2/2 memories retrieved, context recognized, coherence validated
   - **Files:** `src/MemPalace.E2E.Tests/MultiAgentMemoryTests.cs`

3. **Full RAG Pipeline Test** (~120-150 lines)
   - Mine 50-doc corpus (varied topics)
   - User query: "How do we handle distributed caching?"
   - Execute RAG flow: search → inject → respond
   - Verify search R@5 ≥ 96.6% baseline
   - Verify injected context appears in LLM prompt
   - Verify response mentions specific details from injected docs
   - Verify latency <500ms end-to-end
   - **Acceptance:** All 4 assertions pass, baseline maintained, journey complete
   - **Files:** `src/MemPalace.E2E.Tests/RAGPipelineTests.cs`

#### Success Criteria:
- ✅ 3 new E2E test classes created (~300-370 lines total)
- ✅ 10 new test methods (3-4 per test class, covering happy path + edge cases)
- ✅ All tests pass locally (≥90% pass rate)
- ✅ CI/CD integration: `.github/workflows/e2e-tests.yml` updated to run new tests
- ✅ `src/MemPalace.E2E.Tests/E2ETestBase.cs` extended with utilities (reranker mock, RAG helper)
- ✅ Coverage report updated: 66 E2E tests total (91% → 93%+ journey coverage)

#### Dependencies:
- Phase 3D (embedder interface) — COMPLETE ✅
- Phase 3E (testing infrastructure) — COMPLETE ✅
- No external blockers

---

### 🏗️ Workstream B: Documentation & Pattern Library (Parallel, 3-5 days)

**Owner:** Deckard (Lead) + Rachael (DevRel optional)  
**Effort:** 3-5 days

#### Tasks:

1. **Phase 4 Journey Guides** (~5 KB)
   - Add 3 new guides to `docs/guides/`:
     - `reranking-workflow.md` — How to plug in LLM rerankers
     - `agent-memory-diary.md` — Multi-turn agent patterns with MemPalace
     - `rag-integration-guide.md` — Complete RAG pipeline walkthrough
   - Each guide: explanation + code example + performance SLOs

2. **SKILL_PATTERNS.md Update** (~2 KB)
   - Add Pattern 6: "LLM Reranking for Quality"
   - Add Pattern 7: "Agent Memory Diaries"
   - Add Pattern 8: "RAG Context Injection"
   - Cross-reference new E2E tests as executable examples

3. **Release Notes for v0.15.0** (if needed)
   - Already published (v0.15.0 released in Phase 3)
   - No changes needed unless adding Phase 4 context

#### Success Criteria:
- ✅ 3 journey guides created + published to docs
- ✅ SKILL_PATTERNS.md updated with 3 new patterns
- ✅ E2E tests cross-referenced in guides (users can read + see working tests)
- ✅ All docs pass linting (if applicable)

#### Dependencies:
- Phase 4A (E2E tests) — PARALLEL (can draft concurrently, finalize after tests pass)

---

### 📊 Workstream C: Metrics & Baselines (2-3 days)

**Owner:** Bryant (Tester)  
**Effort:** 2-3 days

#### Tasks:

1. **Baseline Verification**
   - Confirm Phase 3E SLOs still met:
     - LongMemEval R@5: 96.6% (no regression)
     - Wake-up latency: <50ms (10K memories)
     - Unit test pass rate: ≥85.9% (402/468)
   - Document in `.squad/identity/now.md`

2. **E2E Coverage Report**
   - Generate coverage report: 66/66 E2E tests passing (100%)
   - Journey coverage: 93%+ of 6-step workflow + advanced patterns
   - Create `.squad/artifacts/phase4-e2e-coverage.md`

3. **Performance Benchmarks** (optional)
   - Measure RAG pipeline latency (target: <500ms)
   - Measure reranking overhead (target: <200ms for 10 candidates)
   - Log to `.squad/artifacts/phase4-benchmarks.md`

#### Success Criteria:
- ✅ Baselines verified, no regressions
- ✅ Coverage report shows 93%+ journey coverage
- ✅ Benchmarks logged (if measured)

#### Dependencies:
- Phase 4A (E2E tests) — must complete before measuring

---

## Dependency Graph

```
Phase 3 (COMPLETE) ✅
└── v0.15.0 released (NuGet + GitHub)

Phase 4 (KICKOFF) 🚀
├── Workstream A: E2E Advanced Tests (1 week)
│   ├── Reranking Journey (~2 days)
│   ├── Multi-Agent Memory (~2 days)
│   └── Full RAG Pipeline (~2 days)
│
├── Workstream B: Documentation (Parallel, 3-5 days)
│   ├── 3 Journey Guides
│   ├── SKILL_PATTERNS.md update
│   └── (depends on WS-A for code examples)
│
└── Workstream C: Metrics & Baselines (2-3 days)
    └── (depends on WS-A for pass/fail)

Phase 5 (Pending) ⏳
├── Remote Skill Registry
├── Ollama Embedder Support
└── WebSocket MCP Transport
```

---

## Risk Assessment

### High Risk 🔴
1. **LLM Reranker availability** (test uses Claude)
   - **Impact:** Reranking test requires API access
   - **Mitigation:** Mock LLM client for local testing; use real Claude in CI only if env var set
   - **Owner:** Bryant

### Medium Risk 🟡
2. **E2E test flakiness** (determinism)
   - **Impact:** Hash-based embeddings may vary; vector similarity is non-deterministic
   - **Mitigation:** Use fixed ONNX model; set random seed; accept ±5% variance
   - **Owner:** Bryant

3. **RAG pipeline latency** (cloud API calls)
   - **Impact:** If using remote LLM, latency could exceed 500ms budget
   - **Mitigation:** Mock LLM in tests; measure realistic latency separately
   - **Owner:** Bryant

### Low Risk 🟢
4. **Documentation gaps**
   - **Impact:** Guides may need rewrites after tests reveal edge cases
   - **Mitigation:** Draft guides after Phase 4A completes
   - **Owner:** Deckard

---

## Team Assignments

| Agent | Phase 4A (WS-A) | Phase 4B (WS-B) | Phase 4C (WS-C) | Total Effort |
|-------|-----------------|-----------------|-----------------|--------------|
| **Bryant** | E2E Tests (7d) | — | Metrics/Baselines (3d) | 10 days |
| **Deckard** | — | Guides Lead (3d) | — | 3 days |
| **Rachael** | — | Guides Support (opt., 2d) | — | 0-2 days |
| **Scribe** | Logging (continuous) | Logging (continuous) | Logging (continuous) | Continuous |
| **Total** | ~7 eng-days | ~3-5 eng-days | ~2-3 eng-days | **12-15 eng-days** |

**Timeline:** 2-3 weeks (assuming 5-day work weeks, parallel WS-B with WS-A)

---

## Success Metrics

### Phase 4A Exit Criteria (E2E Tests)
- ✅ 3 new E2E test classes implemented (~300-370 lines)
- ✅ 10 new test methods (3-4 per class)
- ✅ ≥90% pass rate locally (9/10 or better)
- ✅ CI/CD integration complete
- ✅ Coverage report: 66 E2E tests, 93%+ journey coverage

### Phase 4B Exit Criteria (Documentation)
- ✅ 3 journey guides published (reranking, agent memory, RAG)
- ✅ SKILL_PATTERNS.md updated with 3 new patterns
- ✅ E2E tests cross-referenced in guides
- ✅ All docs pass linting

### Phase 4C Exit Criteria (Metrics)
- ✅ Phase 3E SLOs verified (R@5 96.6%, latency <50ms, test pass rate ≥85.9%)
- ✅ No regressions detected
- ✅ Coverage report: 93%+ journey coverage documented
- ✅ Performance benchmarks logged (optional)

### v0.15.0 Post-Release Validation
- ✅ All Phase 4 tests passing
- ✅ Advanced journey patterns validated
- ✅ Documentation updated with new patterns
- ✅ Ready for Phase 5 planning

---

## Deferred to Phase 5 / v0.8.0

The following items are **out of scope** for Phase 4:

1. **Remote Skill Registry** (Rachael)
   - Skill search/install from public registry
   - Dependency resolution and versioning
   - **Reason:** Phase 4A-C focused on journey validation, not new features

2. **Ollama Embedder Support** (Roy)
   - Blocked by Microsoft.Extensions.AI.Ollama stable release
   - **Reason:** Deferred from Phase 2; ElBruno.LocalEmbeddings provides local-first alternative

3. **WebSocket MCP Transport** (Tyrell)
   - Advanced MCP protocol extension
   - **Reason:** SSE transport MVP sufficient for v0.15.0; WebSocket is enhancement

4. **Quantization & ANN** (Performance)
   - Vector quantization (int8/bit vectors)
   - Approximate nearest neighbor algorithms
   - **Reason:** Current performance acceptable for <100K memories

---

## Communication Plan

### Phase 4 Kickoff (Today)
- Announce Phase 4 roadmap to team
- Assign Bryant (WS-A), Deckard (WS-B), Scribe (logging)
- Spawn agents to begin work

### Weekly Sync (Mondays)
- Progress review (each agent reports status on WS-A, WS-B, WS-C)
- Blockers discussion
- Scope validation

### Daily Standups (async via .squad/log/)
- What did I complete yesterday?
- What am I working on today?
- Any blockers?

### Phase 4 Completion
- Final verification: All 3 workstreams complete
- Publish Phase 4 completion checkpoint
- Transition planning for Phase 5

---

## Open Questions

1. **Reranking LLM:** Should Phase 4A use Claude mini or mock LLM? (Recommend: mock locally, real Claude in CI)
2. **E2E test target:** 90% pass rate sufficient, or aim for 100%? (Recommend: 90% acceptable, investigate failures)
3. **Documentation priority:** Urgent or can draft alongside Phase 4A? (Recommend: draft during WS-A, finalize after)
4. **Phase 5 timeline:** Ready for kickoff after Phase 4C, or wait for user request?

---

## Appendix: GitHub Issues Mapping

### Phase 4 (Active) 🚀

| Issue # | Title | Workstream | Owner | Status |
|---------|-------|-----------|-------|--------|
| TBD | Add reranking E2E journey tests | A (E2E Tests) | Bryant | 🚀 ACTIVE |
| TBD | Add multi-agent memory E2E tests | A (E2E Tests) | Bryant | 🚀 ACTIVE |
| TBD | Add full RAG pipeline E2E tests | A (E2E Tests) | Bryant | 🚀 ACTIVE |
| TBD | Create reranking workflow guide | B (Docs) | Deckard | 🚀 ACTIVE |
| TBD | Create agent memory diary guide | B (Docs) | Deckard | 🚀 ACTIVE |
| TBD | Create RAG integration guide | B (Docs) | Deckard | 🚀 ACTIVE |
| TBD | Verify Phase 3E SLOs | C (Metrics) | Bryant | 🚀 ACTIVE |

### Phase 5 (Pending) ⏳

| Issue # | Title | Owner | Status |
|---------|-------|-------|--------|
| TBD | Remote Skill Registry (v0.8.0) | Rachael | ⏳ PENDING |
| TBD | Ollama Embedder Support (v0.8.0) | Roy | ⏳ BLOCKED |
| TBD | WebSocket MCP Transport (v0.8.0) | Tyrell | ⏳ PENDING |

---

## Change Log

| Date | Author | Change |
|------|--------|--------|
| 2026-05-04 | Deckard | Phase 4 roadmap created; E2E journey tests identified as Phase 4A |
| 2026-05-04 | Squad | Phase 4 kickoff initiated; workstreams assigned |

---

**Next Review:** 2026-05-11 (Phase 4A progress check)  
**Phase 4 Target:** 2026-05-18 (2-week completion)  
**Phase 4 Status:** ACTIVE 🚀 (3 parallel workstreams)
