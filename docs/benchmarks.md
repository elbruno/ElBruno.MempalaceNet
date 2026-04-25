# MemPalace.NET Benchmarks

## Overview

The `MemPalace.Benchmarks` project provides a harness for evaluating MemPalace.NET's memory recall performance against standard benchmarks. It includes four benchmark suites and micro-benchmarks for performance profiling.

## Quick Start: LongMemEval Parity Validation

To validate MemPalace.NET's recall performance against the standard LongMemEval benchmark:

```bash
# 1. Download the official dataset (500 queries, ~2.5MB)
mkdir -p artifacts/benchmarks
curl -L -o artifacts/benchmarks/longmemeval_s_cleaned.json \
  https://huggingface.co/datasets/xiaowu0162/longmemeval-cleaned/resolve/main/longmemeval_s_cleaned.json

# 2. Run the benchmark with local ONNX embedder (baseline)
dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset artifacts/benchmarks/longmemeval_s_cleaned.json \
  --palace artifacts/benchmarks/palace-local \
  --embedder local

# Expected: R@5 ≥ 91% (target for v0.6.0)
# Python baseline: 96.6% R@5 (with nomic-embed-text)
```

**Interpretation:**
- **R@5 (Recall at 5):** Did the correct session appear in the top-5 search results?
- **Target:** ≥ 91% with local ONNX embedder (allows for embedder variance)
- **Stretch target:** ≥ 95% with nomic-embed-text via Ollama (closest to Python baseline)

See [Parity Results](#parity-results-v060) section below for detailed validation status.

## Benchmark Suites

### 1. LongMemEval
**What it measures:** Long-conversation memory recall across multi-session dialogues.

**Dataset format:** JSONL with conversation turns grouped by `session_id`. Each item has:
- `id`: Unique identifier
- `question`: Query to test recall
- `expected_answer`: The conversation turn to be stored as a memory
- `relevant_memory_ids`: IDs of memories that should be retrieved
- `metadata`: Including `session_id` and `turn` number

**Upstream reference:** Inspired by conversational memory benchmarks in long-context AI research.

### 2. LoCoMo (Long-Context Memory)
**What it measures:** Long-context conversational Q&A over episodic memories.

**Dataset format:** Similar to LongMemEval but with `episode` metadata instead of sessions.

**Upstream reference:** Based on long-context understanding tasks.

### 3. ConvoMem
**What it measures:** Sequential turn-by-turn conversational memory with immediate recall.

**Dataset format:** JSONL with sequential `turn` metadata. Tests whether the system can recall information from earlier turns.

### 4. MemBench
**What it measures:** General memory recall on diverse factual statements.

**Dataset format:** JSONL with `category` metadata (e.g., physics, history, biology). Tests semantic retrieval across domains.

## Obtaining Real Datasets

The synthetic datasets under `datasets-synthetic/` are **smoke fixtures only** (5 items each) for CI and quick validation. They are **not** suitable for evaluation.

Real benchmark datasets should be obtained from:
- **LongMemEval/LoCoMo/ConvoMem:** Check the [MemPalace Python repository](https://github.com/MemPalace/mempalace/tree/main/benchmarks) for dataset links.
- **MemBench:** Community-contributed; check MemPalace discussions or academic papers.

Place real datasets in a directory (e.g., `./datasets/`) with filenames matching the benchmark names (`longmemeval.jsonl`, `locomo.jsonl`, etc.).

### QA status: real parity run (2026-04-25)

Tyrell extended the harness so LongMemEval can now consume the upstream JSON array format and rebuild a fresh haystack per question, matching the Python benchmark's session-level raw mode much more closely.

- Dataset: `https://huggingface.co/datasets/xiaowu0162/longmemeval-cleaned/resolve/main/longmemeval_s_cleaned.json`
- Synthetic control: `dotnet run --project src/MemPalace.Benchmarks -- run longmemeval --dataset src/MemPalace.Benchmarks/datasets-synthetic/longmemeval.jsonl --palace artifacts/benchmarks/palace-synth` ✅
- Real-data command shape: `dotnet run --project src/MemPalace.Benchmarks -- run longmemeval --dataset artifacts/benchmarks/longmemeval_s_cleaned.json --palace artifacts/benchmarks/palace-real --embedder local` ✅ supported

What changed:

- `DatasetLoader` now accepts both the original JSONL fixtures and the upstream LongMemEval JSON array.
- Upstream `haystack_sessions` are normalized into per-question corpus documents using the same session-level "join user turns" rule as the Python benchmark.
- LongMemEval now deletes and recreates its collection for each question so evaluation happens against a fresh haystack instead of one shared corpus.
- CLI commands can now use `--embedder deterministic|local|ollama` instead of always forcing the deterministic smoke embedder.

Remaining parity caveat:

- `--embedder local` uses MemPalace.NET's default ONNX model (`sentence-transformers/all-MiniLM-L6-v2`), which is a **real** embedder but not the Python repo's `nomic` configuration.
- For the closest apples-to-apples run, use `--embedder ollama --model nomic-embed-text` with a running Ollama server.

## Running Benchmarks

### List Available Benchmarks
```bash
dotnet run --project src/MemPalace.Benchmarks -- list
```

### Run a Single Benchmark
```bash
dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset ./datasets/longmemeval.jsonl \
  --palace ./_benchmark_palace \
  --max 100
```

Options:
- `--dataset`: Path to a benchmark dataset file (`.jsonl` fixtures or supported JSON array datasets such as upstream LongMemEval)
- `--palace`: Directory for palace storage (will be created if needed)
- `--max`: Optional limit on number of items to process
- `--embedder`: `deterministic` (default smoke mode), `local` (real ONNX model), or `ollama`
- `--model`: Optional embedder model override
- `--endpoint`: Optional Ollama endpoint override (default `http://localhost:11434`)

### Run a Real LongMemEval Session-Level Benchmark
```bash
dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset ./artifacts/benchmarks/longmemeval_s_cleaned.json \
  --palace ./artifacts/benchmarks/palace-real \
  --embedder local
```

For closer parity with the Python benchmark's `nomic` run:

```bash
dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset ./artifacts/benchmarks/longmemeval_s_cleaned.json \
  --palace ./artifacts/benchmarks/palace-real \
  --embedder ollama \
  --model nomic-embed-text
```

### Run All Benchmarks
```bash
dotnet run --project src/MemPalace.Benchmarks -- run-all \
  --datasets-dir ./datasets \
  --palace ./_benchmark_palace \
  --out results.json
```

Options:
- `--datasets-dir`: Directory containing `{benchmark_name}.jsonl` files
- `--palace`: Palace storage directory
- `--max`: Optional limit per benchmark
- `--out`: JSON file to save results (optional)
- `--embedder`, `--model`, `--endpoint`: Same as `run`

### Run Micro-Benchmarks
```bash
dotnet run --project src/MemPalace.Benchmarks -c Release -- micro
```

**Note:** Micro-benchmarks use BenchmarkDotNet and should be run in Release mode. They measure:
- Embedding throughput (embeds/sec)
- Vector query latency (1k/10k vectors, top-10 queries)

Micro-benchmarks use a deterministic embedder and don't require real models.

## Result Schema

```json
{
  "BenchmarkName": "longmemeval",
  "TotalQueries": 100,
  "Correct": 97,
  "Recall": 0.9700,
  "Precision": 0.8500,
  "F1": 0.9057,
  "NdcgAt10": 0.9200,
  "TotalDuration": "00:01:23.4567890",
  "ExtraMetrics": {}
}
```

**Metrics:**
- **Recall@10:** Fraction of relevant memories retrieved in top-10 results (averaged across queries).
- **Precision@10:** Fraction of retrieved memories that are relevant (averaged).
- **F1:** Harmonic mean of precision and recall.
- **NDCG@10:** Normalized Discounted Cumulative Gain at k=10 (accounts for ranking quality).
- **Correct:** Number of queries with at least one relevant memory in top-10.

## Synthetic Fixtures

The `datasets-synthetic/` directory contains minimal smoke fixtures for each benchmark. These are used in CI to verify the harness works, **not** for performance evaluation.

Each fixture has 5 items and uses simple, hand-crafted data.

## Development

### Adding a New Benchmark

1. Create a class in `Runners/` implementing `IBenchmark`.
2. Extend `BenchmarkBase` to reuse ingestion/query logic.
3. Add the benchmark to `ListCommand.GetAllBenchmarks()`.
4. Add a synthetic fixture under `datasets-synthetic/{name}.jsonl`.
5. Write tests in `MemPalace.Tests/Benchmarks/`.

### Testing

Run the benchmark test suite:
```bash
dotnet test --filter "FullyQualifiedName~Benchmarks"
```

Tests use:
- **InMemoryBackend** for fast, reproducible execution
- **DeterministicEmbedder** (hash-based vectors, no real model)
- Synthetic datasets (no network/downloads)

### CI Integration

The benchmark smoke tests run in CI to ensure the harness doesn't break. Real benchmarks are run separately by maintainers before releases.

## Parity Targets

From the Python MemPalace implementation:
- **LongMemEval R@5:** ≥ 96.6% (raw embedding retrieval)
- **LoCoMo R@10:** ≥ 60.3% (session-based, no reranker)

These targets are based on the upstream Python implementation with `nomic-embed-text`. .NET results may vary depending on the embedder used; `--embedder ollama --model nomic-embed-text` is the closest current match.

## Parity Results (v0.6.0)

**Status:** 🚧 Pending full validation run

**Target baseline run:** MemPalace.NET with `sentence-transformers/all-MiniLM-L6-v2` (ONNX)

| Metric | Score | Target | Status |
|--------|-------|--------|--------|
| Recall@5 | _Pending_ | ≥ 91% | 🚧 Validation in progress |
| Recall@10 | _Pending_ | ≥ 95% | 🚧 Validation in progress |
| NDCG@10 | _Pending_ | ≥ 0.85 | 🚧 Validation in progress |
| Duration | _Pending_ | < 10 min | 🚧 Validation in progress |

**Validation command:**
```bash
# 1. Download LongMemEval dataset
curl -L -o artifacts/benchmarks/longmemeval_s_cleaned.json \
  https://huggingface.co/datasets/xiaowu0162/longmemeval-cleaned/resolve/main/longmemeval_s_cleaned.json

# 2. Run baseline validation
dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset artifacts/benchmarks/longmemeval_s_cleaned.json \
  --palace artifacts/benchmarks/palace-local \
  --embedder local
```

**Expected results:**
- R@5: 88-94% (due to embedder difference: MiniLM 384-dim vs nomic 1536-dim)
- Python baseline: 96.6% R@5 with nomic-embed-text
- Delta: -2 to -8 percentage points (embedder variance is expected)

**For closest parity with Python baseline:**
```bash
# Requires Ollama with nomic-embed-text model
ollama pull nomic-embed-text

dotnet run --project src/MemPalace.Benchmarks -- run longmemeval \
  --dataset artifacts/benchmarks/longmemeval_s_cleaned.json \
  --palace artifacts/benchmarks/palace-nomic \
  --embedder ollama \
  --model nomic-embed-text
```

**Expected with nomic embedder:** 95-97% R@5 (within 1-2 percentage points of Python)
