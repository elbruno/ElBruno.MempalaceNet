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
