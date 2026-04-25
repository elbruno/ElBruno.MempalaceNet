# Benchmark Parity Triage

## When to use

Use this when a benchmark harness exists but parity with an upstream reference implementation is unproven.

## Steps

1. Run the repo's existing smoke/control benchmark first to prove the harness still executes.
2. Locate the upstream documented dataset source and fetch the real dataset without committing large artifacts.
3. Attempt the benchmark with the real dataset exactly as documented.
4. Compare three things before trusting any score:
   - **dataset shape** (JSON vs JSONL, field names, nesting),
   - **runtime embedder/model** (real model vs deterministic/mock embedder),
   - **benchmark semantics** (shared corpus vs fresh corpus per query, same top-k, same scoring).
5. If the full run is blocked, codify the blocker in tests/docs with the exact failure mode and command line used.

## Pattern

Synthetic smoke data can prove the harness works while still hiding parity blockers.
The fastest honest QA path is: control run succeeds, real-data run fails, document the first concrete incompatibility, then list any deeper semantic mismatches found in code review.
