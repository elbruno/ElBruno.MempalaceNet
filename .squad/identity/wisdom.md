---
last_updated: 2026-04-24T16:11:53.381Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** Smoke tests verify harness works; real benchmarks require manual execution with real data.  
**Context:** When building benchmark infrastructure. Synthetic fixtures (5 items) catch framework bugs in CI (no downloads, no model deps). Real datasets (1k+ items) must run separately to validate parity. This avoids flaky CI while still gating the product claim.

**Pattern:** Use DeterministicEmbedder (hash-based vectors) for reproducible test data; embed real with model only in integration/parity tests.  
**Context:** When writing tests for vector-based systems. Deterministic embeddings isolate logic testing (search algorithms, ranking, filtering) from embedder correctness. This keeps unit tests fast and flaky-free while still covering integration.

**Pattern:** Backend conformance suite (xUnit theory) can be shared across implementations.  
**Context:** When adding new backends (SQLite, Qdrant, Chroma). Define conformance as a base class with ~25 test theories. Each backend implementation runs the same tests, ensuring contract compliance. This is cheaper than writing backend-specific tests.

**Pattern:** E2E workflow tests (init → ingest → search) must use real I/O and real dependencies.  
**Context:** When testing user-facing workflows. Unit tests catch logic bugs; e2e tests catch integration bugs (DI wiring, file I/O, state propagation). At least one e2e per major feature (palace workflow, CLI commands, agent diary).

**Pattern:** CLI tests should verify both parsing AND execution; parsing alone misses runtime bugs.  
**Context:** When testing CLI apps. Parsing tests (Spectre.Console argument validation) are cheap and pass often. Execution tests (actually run commands, verify output/side effects) catch DI bugs, file system issues, error handling. Both are needed for confidence.
