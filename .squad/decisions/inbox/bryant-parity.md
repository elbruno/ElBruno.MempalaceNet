# Bryant — parity benchmark blocker

## Proposed decision

Do not claim a reproducible .NET parity result from `MemPalace.Benchmarks` until the harness can:

1. ingest the upstream benchmark formats directly (LongMemEval JSON array, LoCoMo source JSON, etc.),
2. run with a configurable real embedder instead of the hardcoded `DeterministicEmbedder`, and
3. mirror upstream benchmark semantics (for LongMemEval, rebuild a fresh haystack per question instead of querying one shared collection).

## Why this is in the inbox

I downloaded the real upstream LongMemEval dataset and attempted a repo-local run on 2026-04-25.
The command failed at the loader boundary because the current harness expects JSONL, while the real dataset is a JSON array with different field names.
Even if format conversion were added externally, the current CLI still uses `DeterministicEmbedder`, so the result would remain a smoke/integration run rather than a parity benchmark.
