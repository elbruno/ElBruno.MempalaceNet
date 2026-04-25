# Bryant — History

## Core Context
- **Project:** MemPalace.NET — port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** Tester / QA + Reviewer
- **Test stack:** xUnit, FluentAssertions, NSubstitute. Coverlet for coverage.
- **Parity targets:** LongMemEval R@5 ≥ 96.6% (raw), LoCoMo R@10 ≥ 60.3% (session, no rerank). To be re-validated on .NET embedder.

## Learnings

### 2026-04-24 — Phase 9: Benchmark Harness

**Task:** Create `MemPalace.Benchmarks` project with harness for four benchmarks (LongMemEval, LoCoMo, ConvoMem, MemBench), micro-benchmarks, and CLI.

**Implementation:**
- Created new console app project (`mempalacenet-bench` tool)
- Core harness: `IBenchmark` interface with `BenchmarkContext`, `BenchmarkResult`, `DatasetItem`
- `DatasetLoader`: async JSONL streaming (avoids loading 10k+ items into memory)
- `Metrics` class: pure functions for Recall@k, Precision@k, F1, NDCG@k
- `BenchmarkBase` abstract class: shared ingestion/query/scoring logic across benchmarks
- Four benchmark runners implementing specific ingestion strategies:
  - LongMemEval: session-grouped conversation turns
  - LoCoMo: episodic conversation memories
  - ConvoMem: sequential turn-by-turn with ordering
  - MemBench: flat general statements
- Micro-benchmarks using BenchmarkDotNet:
  - `EmbeddingThroughputBench`: embeds/sec at batch sizes 10/100
  - `VectorQueryLatencyBench`: query latency with 1k/10k vectors
- CLI (Spectre.Console.Cli): `list`, `run`, `run-all`, `micro` commands
- Synthetic smoke datasets: 5-item JSONL fixtures per benchmark (CI-safe, no downloads)
- Tests (20 total):
  - `MetricsTests`: 14 tests covering recall/precision/F1/NDCG edge cases
  - `DatasetLoaderTests`: 4 tests for JSONL parsing, pagination, empty lines
  - `LongMemEvalBenchmarkSmokeTests`: 2 tests running all benchmarks end-to-end with synthetic data
- `DeterministicEmbedder`: shared helper for benchmarks (hash-based vectors, no model required)

**Key decisions:**
1. **JSONL streaming over batch loading** — supports large datasets (10k+ items) without memory issues
2. **BenchmarkDotNet for micro-benchmarks** — industry standard, reliable tooling
3. **Synthetic fixtures for CI** — all tests pass without downloading 100MB datasets or calling real models
4. **Shared DeterministicEmbedder** — reproducible, deterministic embeddings (hash-based) for testing
5. **ICollection.QueryAsync expects IReadOnlyList<ReadOnlyMemory<float>>** — wrap single vector in array

**Technical notes:**
- Avoid `reader.EndOfStream` in async methods (CA2024 analyzer error) — use `ReadLineAsync()` null check instead
- `IBackend` doesn't have `DropCollectionAsync` — use unique palace IDs per run instead of dropping
- `IEmbedder` interface: `ValueTask` (not `Task`), requires `ModelIdentity` and `Dimensions` properties
- Metrics calculation: NDCG uses log2(position+2) for 1-indexed positions

**Test results:**
- 150/150 tests green (129 existing + 20 new + 1 new smoke test)
- `mempalacenet-bench list` works
- `mempalacenet-bench run longmemeval --dataset <synthetic> --palace <tmp>` runs end-to-end

**Files created:**
- 29 new files (1602 lines)
- `MemPalace.Benchmarks/` project with Core, Scoring, Runners, Micro, Commands
- `MemPalace.Tests/Benchmarks/` test suite
- `docs/benchmarks.md` documentation

**Exit criteria met:**
- Build clean
- All tests green
- CLI commands functional
- Documentation complete

---

### 2026-04-24 — QA Audit: Missing Coverage Assessment

**Task:** Audit the codebase for gaps in test coverage, benchmarks, parity validation, and CI/CD wiring. Call out the most important QA gaps.

**Audit Summary:**
- **150 tests passing** ✅ across 9 modules (backends, search, mining, benchmarks, agents, MCP, CLI, AI, KG)
- **Baseline unit/module tests: Complete** ✅ (40 backend, 9 mining, 5 search, 16 KG, 13 AI, 6 agents, 5 MCP, 10 CLI parsing)
- **Parity validation: Not yet run** ⚠️ (harness exists, synthetic smoke tests only)
- **E2E integration: Missing** ❌ (no palace init→mine→search workflow test)
- **CLI execution: Parsing only** ⚠️ (10 tests verify argument parsing, zero tests verify command behavior)
- **LlmReranker: Untested** ❌ (implementation exists, no unit tests)
- **Code coverage: Not reported** ❌ (no Coverlet gate in CI)

**Critical Gaps (block v0.1 release):**

1. **Parity validation vs Python reference**
   - LongMemEval/LoCoMo/ConvoMem harness exists, synthetic smoke datasets pass
   - Real datasets NOT tested (must run against benchmark.jsonl from Python repo)
   - Target: LongMemEval R@5 ≥ 96.6%, LoCoMo R@10 ≥ 60.3% (raw, no rerank)
   - Impact: Without this, cannot claim .NET port achieves parity

2. **End-to-end palace workflow**
   - Tests cover individual components (backends, search, mining) in isolation
   - No test validates: `init → mine → search → recall` full cycle
   - Missing: state propagation across layers (embedder → backend → search)
   - Impact: Core product workflow untested in integration

3. **CLI command execution**
   - 10 CLI parsing tests only (args validated but commands not run)
   - Missing: `mempalacenet init`, `mempalacenet mine`, `mempalacenet search` actually work
   - Missing: error cases (invalid paths, permission errors, malformed files)
   - Impact: Users cannot verify CLI works before v0.1

**High-Priority Gaps (improves quality pre-release):**

4. **LlmReranker unit tests** — interface defined, implementation exists, zero coverage
5. **Error recovery** — no tests for embedder failures, backend crashes, dimension mismatches
6. **Performance baseline** — micro-benchmarks exist (EmbeddingThroughput, VectorQueryLatency) but no CI gates or trending
7. **MCP end-to-end** — tool discovery works, no e2e client test (can MCP client call tools and get results?)

**Medium-Priority Gaps (post-v0.1):**

8. **Code coverage reporting** — add Coverlet to CI, set ≥80% gate
9. **Integration tests with agents** — palace + knowledge graph + agent diary all together
10. **Regression baseline** — save micro-benchmark results, compare on every build

**Files analyzed:**
- 23 test files, 150 tests across 9 projects
- Benchmarks: `LongMemEvalBenchmarkSmokeTests`, `MetricsTests`, `DatasetLoaderTests`
- CI: `.github/workflows/ci.yml` runs build + test, no coverage/perf gates
- Docs: `docs/benchmarks.md` specifies parity targets but not validation procedure

**Recommendations for v0.1:**
1. **MUST:** Run real benchmark (pick one dataset, validate Recall@5 or R@10 is within 5pp of Python target)
2. **MUST:** Add CLI e2e test (init + mine 1 file + search + verify results)
3. **SHOULD:** Add LlmReranker unit tests (mock IChatClient, verify ranking logic)
4. **SHOULD:** Document manual validation steps (which datasets to use, expected ranges)

### 2026-04-25: Cross-Agent Update — Deckard Roadmap Audit
 
**Cross-Agent Finding:** Deckard completed roadmap audit confirming Phases 0-10 delivered. v0.1.0 ready after 3 small fixes.

**Key Recommendations for QA:**
1. **CI workflow fix** (Deckard): Add main + PR triggers → enables continuous benchmark validation (not just on tags)
2. **Real benchmark execution** (Bryant recommendation stands): Run LongMemEval.jsonl before v0.1 tag to validate parity claim
3. **CLI e2e test**: Gap identified by Deckard's doc/feature audit confirms Bryant's Gap 3 concern
 
**Status:** Decisions merged to formal record (Scribe session). Inbox cleared. Ready for Bruno's fixes.

### 2026-04-25 — Real Parity Benchmark Attempt

**Task:** Run a real parity benchmark from this repo using an upstream dataset, capture the outcome, and document the result or blocker.

**What I ran:**
- Verified baseline with `dotnet test src\MemPalace.slnx`
- Downloaded upstream LongMemEval dataset from Hugging Face: `longmemeval_s_cleaned.json`
- Control run succeeded on `src/MemPalace.Benchmarks/datasets-synthetic/longmemeval.jsonl`
- Real-data run failed immediately in `DatasetLoader`

**Concrete findings:**
- Upstream LongMemEval ships as a JSON array with `question_id`, `answer`, `answer_session_ids`, `haystack_sessions`, etc.
- `src/MemPalace.Benchmarks/Core/DatasetLoader.cs` only accepts line-delimited JSONL with `id`, `expected_answer`, `relevant_memory_ids`, and `metadata`.
- `src/MemPalace.Benchmarks/Commands/RunCommand.cs` and `RunAllCommand.cs` hardcode `DeterministicEmbedder`, so current CLI runs are smoke benchmarks, not parity runs against the Python embedding baseline.
- `src/MemPalace.Benchmarks/Runners/BenchmarkBase.cs` queries one shared collection, while upstream LongMemEval rebuilds a fresh haystack per question. That is another parity mismatch even after format conversion.

**Repo updates:**
- Added `DatasetLoaderTests.LoadAsync_UpstreamLongMemEvalJsonArray_ThrowsJsonException` to lock in the currently observed blocker.
- Updated `docs/benchmarks.md` with the real-dataset attempt, exact failure, and parity blockers.
- Wrote decision inbox note for follow-up.

**Key file paths:**
- `docs/benchmarks.md`
- `src/MemPalace.Benchmarks/Core/DatasetLoader.cs`
- `src/MemPalace.Benchmarks/Commands/RunCommand.cs`
- `src/MemPalace.Benchmarks/Commands/RunAllCommand.cs`
- `src/MemPalace.Benchmarks/Runners/BenchmarkBase.cs`
- `src/MemPalace.Tests/Benchmarks/DatasetLoaderTests.cs`

---

### 2026-04-25: Cross-Agent Update — Deckard Roadmap Audit & v0.1.0 Release Status

**Input:** Deckard completed comprehensive roadmap audit. All 10 phases delivered/in progress. 150/150 tests passing. v0.1.0 ready after 3 small doc fixes (<1 hour).

**Doc fixes completed by Deckard:**
1. README: "29 tools" → "7 tools in v0.1" (now accurate)
2. RELEASE-v0.1.md: tool count corrected
3. README quick start: removed `wake-up` command (not yet implemented, Phase 11)

**Impact on Bryant's work:** Parity benchmarks correctly deferred to Phase 11+. v0.1 release accurate and ready. Synthetic smoke test suite validates harness mechanics correctly (no real model/dataset dependency needed for CI).

**Status:** ✅ v0.1.0 ready for tagging. Parity validation scope clarified for Phase 11+.

