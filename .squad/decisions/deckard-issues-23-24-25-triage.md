# Triage Decision: Issues #23, #24, #25 (OpenClawNet Phase 2B Dependencies)

**Date:** 2026-04-28  
**Architect:** Deckard  
**Status:** Pending GitHub Action

---

## Context

Three new feature requests from OpenClawNet Phase 2B integration. All request shared abstractions/utilities that OpenClawNet currently implements in-house. Moving these to MempalaceNet standardizes patterns across the ecosystem.

---

## Triage Assignments

### Issue #25: IVectorFormatValidator for sqlite-vec BLOB standardization
**Assignment:** Tyrell (Storage specialist)  
**Priority:** High  
**Scope:** Storage layer interface for BLOB validation in sqlite-vec integration  

**Architectural Notes:**
- Interface belongs in MempalaceNet.Storage (aligns with vector storage domain)
- ValidationResult pattern matches existing error-handling conventions
- Support for variable dimensions is essential for multi-embedder scenarios (Ollama nomic-embed-text: 768 dims, OpenAI text-embedding-3-small: 1536 dims)
- This is a pure validation layer — no sqlite-vec runtime dependencies (consumers bring their own sqlite-vec)
- Critical for data integrity: prevents corrupted vectors from entering storage

**Dependencies:** None (standalone interface)

**Acceptance Criteria:**
- IVectorFormatValidator interface in MempalaceNet.Storage
- SqliteVecBlobValidator reference implementation
- Support for variable embedding dimensions
- 15+ unit tests covering valid/invalid formats, edge cases
- XML documentation with examples

**Rationale:** Blocks OpenClawNet Phase 2B production readiness. Vector corruption is subtle and hard to debug — validation must happen at the MempalaceNet layer.

---

### Issue #24: PerformanceBenchmark utilities for SLA tracking
**Assignment:** Rachael (CLI/Diagnostics specialist)  
**Priority:** Medium-High  
**Scope:** Diagnostics framework for latency measurement and SLA compliance tracking  

**Architectural Notes:**
- New namespace: MempalaceNet.Diagnostics (distinct from Core/Storage)
- Percentile calculation must be deterministic and testable (no approximation algorithms)
- Report generation (markdown + JSON) supports both human and machine consumption
- SLA validation helpers align with OpenClawNet's 200ms enrichment budget pattern (40% of 500ms agent spawn)
- Thread-safe for concurrent benchmarking scenarios

**Dependencies:** None (pure diagnostics layer)

**Acceptance Criteria:**
- PerformanceBenchmark class in MempalaceNet.Diagnostics
- Percentile calculation (P50, P95, P99, P100)
- SLA validation helpers (ValidateSLA method)
- Report generation (markdown, JSON)
- 10+ unit tests covering edge cases (empty datasets, single-sample, outliers)
- XML documentation with usage examples

**Rationale:** Standardizes SLA measurement across MempalaceNet ecosystem. OpenClawNet needs this for semantic re-rank <100ms P95, health check <50ms P95 tracking.

---

### Issue #23: IEmbedderHealthCheck interface for embedder monitoring
**Assignment:** Roy (AI/Agent specialist)  
**Priority:** High  
**Scope:** Health check abstraction for embedder monitoring (Ollama, OpenAI, custom)  

**Architectural Notes:**
- Interface belongs in MempalaceNet.Core (shared by all embedder implementations)
- 100ms timeout pattern aligns with OpenClawNet semantic skill injection SLA
- EmbedderHealthStatus struct supports both sync checks and timeout reporting (IsHealthy, ResponseTime, ErrorMessage)
- Built-in implementations (OllamaHealthCheck, OpenAIHealthCheck) provide reference patterns for consumers
- Integrates with Microsoft.Extensions.AI abstraction (IChatClient, IEmbeddingGenerator)

**Dependencies:** Existing embedder abstractions (Microsoft.Extensions.AI integration)

**Acceptance Criteria:**
- IEmbedderHealthCheck interface in MempalaceNet.Core
- Built-in implementations: OllamaHealthCheck, OpenAIHealthCheck
- 10+ unit tests covering timeout, failure, and success paths
- XML documentation with example usage
- Mock-friendly design for consumer testing

**Rationale:** Enables production-ready agent deployments with fallback logic. OpenClawNet's semantic skill injection layer needs to react gracefully when embedders are unavailable or timing out.

---

## GitHub Actions Required

**Commands to run:**

```bash
# Issue #25 (Tyrell)
gh issue comment 25 --body "## Triage Summary

**Scope:** Storage layer interface for BLOB validation in sqlite-vec integration. Critical for data integrity across MempalaceNet ecosystem.

**Assignment:** @Tyrell (Storage specialist)

**Architectural Notes:**
- Interface belongs in MempalaceNet.Storage (aligns with vector storage domain)
- ValidationResult pattern matches existing error-handling conventions
- Support for variable dimensions is essential for multi-embedder scenarios
- This is a pure validation layer — no sqlite-vec runtime dependencies

**Dependencies:** None (standalone interface)

**Priority:** High — blocks OpenClawNet Phase 2B production readiness

**Next Steps:** Tyrell to implement IVectorFormatValidator with SqliteVecBlobValidator reference implementation. 15+ unit tests required per acceptance criteria.

---
*Triaged by Deckard | Squad: Tyrell*"

gh issue edit 25 --add-label "squad,squad:tyrell,feature,high-priority"

# Issue #24 (Rachael)
gh issue comment 24 --body "## Triage Summary

**Scope:** Diagnostics framework for latency measurement and SLA compliance tracking. Reusable across MempalaceNet consumers.

**Assignment:** @Rachael (CLI/Diagnostics specialist)

**Architectural Notes:**
- New namespace: MempalaceNet.Diagnostics (distinct from Core/Storage)
- Percentile calculation must be deterministic and testable
- Report generation (markdown + JSON) supports both human and machine consumption
- SLA validation helpers align with OpenClawNet's 200ms enrichment budget pattern

**Dependencies:** None (pure diagnostics layer)

**Priority:** Medium-High — standardizes SLA tracking across ecosystem

**Next Steps:** Rachael to implement PerformanceBenchmark with P50/P95/P99/P100 stats. 10+ unit tests covering edge cases (empty datasets, single-sample, outliers).

---
*Triaged by Deckard | Squad: Rachael*"

gh issue edit 24 --add-label "squad,squad:rachael,feature"

# Issue #23 (Roy)
gh issue comment 23 --body "## Triage Summary

**Scope:** Health check abstraction for embedder monitoring (Ollama, OpenAI, custom). Enables graceful degradation in agent scenarios.

**Assignment:** @Roy (AI/Agent specialist)

**Architectural Notes:**
- Interface belongs in MempalaceNet.Core (shared by all embedder implementations)
- 100ms timeout pattern aligns with OpenClawNet semantic skill injection SLA
- EmbedderHealthStatus struct supports both sync checks and timeout reporting
- Built-in implementations (OllamaHealthCheck, OpenAIHealthCheck) provide reference patterns

**Dependencies:** Existing embedder abstractions (Microsoft.Extensions.AI integration)

**Priority:** High — enables production-ready agent deployments with fallback logic

**Next Steps:** Roy to implement IEmbedderHealthCheck + concrete implementations. 10+ unit tests covering timeout, failure, and success paths. Mock-friendly design for consumer testing.

---
*Triaged by Deckard | Squad: Roy*"

gh issue edit 23 --add-label "squad,squad:roy,feature,high-priority"
```

---

## Decision Rationale

**Why separate namespaces?**
- MempalaceNet.Storage: BLOB validation (domain: vector storage)
- MempalaceNet.Diagnostics: Performance benchmarking (domain: observability)
- MempalaceNet.Core: Health checks (domain: embedder abstractions)

This separation maintains clean boundaries and avoids namespace pollution.

**Why reference implementations?**
All three interfaces include concrete implementations (SqliteVecBlobValidator, OllamaHealthCheck, OpenAIHealthCheck) to provide:
1. Reference patterns for consumers
2. Immediate utility out-of-the-box
3. Test coverage for interface contracts

**Why high priority for #23 and #25?**
Both block OpenClawNet Phase 2B production deployments:
- #25: Vector corruption is subtle and hard to debug
- #23: Agent deployments need graceful embedder fallback

**Why medium-high for #24?**
Important for standardization but not a blocker. OpenClawNet can proceed with in-house benchmarking temporarily.

---

## Next Actions

1. Run GitHub CLI commands above to post comments and labels
2. Notify squad members (Tyrell, Rachael, Roy) via GitHub notifications
3. Squad members acknowledge assignments and begin implementation
4. Track progress in .squad/orchestration-log

---

**Architect Sign-off:** Deckard  
**Date:** 2026-04-28
