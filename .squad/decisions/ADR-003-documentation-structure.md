# ADR-003: Documentation Structure for 3 Critical Developer Guides

**Status:** PROPOSED  
**Decision Owner:** Deckard (Lead / Architect)  
**Date:** 2026-04-29  
**Affected Parties:** Content team (Scribe), CLI/UX (Rachael), Backend (Tyrell)

---

## Context

MemPalace.NET has solid foundation documentation (architecture, backends, AI integration, CLI) and runnable examples. However, external developers lack:

1. **Hands-on guide for library developers** — integrating MemPalace.Core types (IBackend, ICollection, IEmbedder) into custom .NET applications
2. **Embedder swap / pluggability reference** — how to switch from ONNX to cloud providers and implement custom embedders
3. **Skill creation deep-dive** — manifest schema, custom skill development, publishing workflow

These gaps prevent smooth onboarding for:
- Internal skill builders (Roy, Rachael)
- External OSS contributors
- Enterprise integrators

---

## Proposal: 3-Tier Documentation Structure

### 1. **C# Library Developer Guide**
**File:** `docs/guides/csharp-library-developers.md`  
**Length:** ~80 lines (reference, not tutorial)  
**Audience:** .NET developers building custom backends, plugins, or integrations

**Outline:**
- Intro: Why use MemPalace Core as a library
- Section 1: Core types (IBackend, ICollection, IEmbedder interface contracts)
- Section 2: Dependency injection setup (DI container registration pattern)
- Section 3: Custom backend stub (non-functional outline showing IBackend + ICollection impl)
- Section 4: Custom embedder stub (implementing IEmbedder for a new provider)
- Section 5: Error handling (BackendException hierarchy)
- **Code samples:** Live in `examples/CustomBackendTemplate/` and `examples/CustomEmbedderTemplate/` (NEW)

**Navigation:** Linked from `docs/README.md` under "Extensibility" section; linked from README.md Quick Links

---

### 2. **Embedder Pluggability Guide**
**File:** `docs/guides/embedder-pluggability.md`  
**Length:** ~100 lines (config + samples)  
**Audience:** Library users and skill developers swapping embedding providers

**Outline:**
- Intro: Available embedders (Local/ONNX, Ollama, OpenAI, Azure OpenAI)
- Section 1: Configuration matrix (provider comparison table)
- Section 2: DI setup examples (four code blocks, one per provider)
- Section 3: CLI tool configuration (via `--embedder` flag, config.json)
- Section 4: Implementing custom embedder (abstract class, contract)
- Section 5: Troubleshooting (model mismatches, API key issues)
- **Code samples:** CLI examples in guide; C# stubs linked to `examples/CustomEmbedderTemplate/`

**Navigation:** Linked from `docs/ai.md` section "Embedder Pluggability"; linked from README.md; accessible from CLI docs

---

### 3. **Skill Integration Deep Dive**
**File:** `docs/guides/skill-development-guide.md`  
**Length:** ~120 lines (manifest + tooling)  
**Audience:** Skill developers, internal team members (Roy, Rachael, Bryant)

**Outline:**
- Intro: Skill manifest format + folder structure
- Section 1: Manifest schema reference (skill.json structure + validation)
- Section 2: Folder layout (src/, README.md, manifest, dependencies.txt)
- Section 3: Skill entry points (PowerShell scripts, CLI integration, MCP tools)
- Section 4: Custom skill template (skeleton repo link)
- Section 5: Publishing & discovery (CLI registry, GitHub skill marketplace)
- Section 6: Testing skills locally (CLI commands, debugging)
- **Code samples:** Manifest examples in guide; full skill template in `examples/CustomSkillTemplate/` (NEW)

**Navigation:** Linked from `docs/guides/skill-manifest-schema.md`; linked from README.md Skill section; referenced from `docs/cli.md`

---

## File Structure (Proposed)

```
docs/
├── guides/
│   ├── csharp-library-developers.md         [NEW]
│   ├── embedder-pluggability.md             [NEW]
│   ├── skill-development-guide.md           [NEW]
│   ├── skill-manifest-schema.md             [EXISTING — keep as reference]
│   └── ... (other guides)
├── README.md                                 [UPDATED: add links to 3 new guides]
├── architecture.md                          [EXISTING]
├── ai.md                                    [EXISTING — link to embedder-pluggability.md]
└── ... (other docs)

examples/
├── README.md                                 [UPDATED: add 2 new templates]
├── SimpleMemoryAgent/                       [EXISTING]
├── SemanticKnowledgeGraph/                 [EXISTING]
├── CustomBackendTemplate/                  [NEW: skeleton IBackend + ICollection impl]
├── CustomEmbedderTemplate/                 [NEW: skeleton IEmbedder impl]
└── CustomSkillTemplate/                    [NEW: manifest + entry point + README template]

root/
└── README.md                                 [UPDATED: add "Extensibility" quick link]
```

---

## Navigation & Linking Strategy

### From `docs/README.md`
Add a new **Extensibility** section:

```markdown
### Extensibility & Custom Development
- [C# Library Developers](guides/csharp-library-developers.md) — Core types, DI, custom backends/embedders
- [Embedder Pluggability](guides/embedder-pluggability.md) — Swap providers, custom implementations
- [Skill Development](guides/skill-development-guide.md) — Manifest, folder structure, publishing
```

### From root `README.md`
Add to Quick Start or Architecture section:

```markdown
## Building Custom Integrations

- **[Extend with Custom Backends](docs/guides/csharp-library-developers.md)** — Implement IBackend for any storage layer
- **[Swap Embedders](docs/guides/embedder-pluggability.md)** — Local ONNX, Ollama, OpenAI, Azure, or custom
- **[Create Skills](docs/guides/skill-development-guide.md)** — Package custom logic as reusable skills
```

### From `docs/ai.md`
Update section "Embedder Pluggability" to link:

```markdown
**Full guide:** [Embedder Pluggability](guides/embedder-pluggability.md)
```

### From `docs/cli.md`
Add link in skills section to skill-development-guide.md

---

## Code Sample Locations

| Sample | Location | Purpose |
|--------|----------|---------|
| IBackend stub | `examples/CustomBackendTemplate/` | Teach contract, skeleton only |
| IEmbedder stub | `examples/CustomEmbedderTemplate/` | Teach contract, skeleton only |
| Skill template | `examples/CustomSkillTemplate/` | Full manifest + entry point + README |
| CLI embedder config | Inline in `docs/guides/embedder-pluggability.md` | Reference, not runnable |
| Manifest examples | Inline in `docs/guides/skill-development-guide.md` | Reference, not runnable |

**Rationale:** Complex stubs (backend, embedder) live in `examples/` as runnable projects. Simple configs (CLI, manifest examples) stay in guides as inline code blocks.

---

## Decision

**Approved structure:**
1. Create 3 new guide files under `docs/guides/`
2. Create 3 new example templates under `examples/`
3. Update `docs/README.md` with "Extensibility" section
4. Update root `README.md` with quick links to guides
5. Update `docs/ai.md` to cross-reference embedder pluggability guide

**Timeline:** Guides + templates to be written in Phase 3 / Release Prep (Weeks 4-5)  
**Owner:** Deckard (architecture), Tyrell (backend samples), Roy (skill template)

---

## Constraints

- All guides under `docs/` per project rules
- All code samples under `examples/` with runnable projects
- Cross-references must be relative paths (portable)
- Guides are *reference + training*, not exhaustive API docs (defer to code comments)

---

## Next Steps

1. **Immediate:** Merge this ADR into `.squad/decisions/` (tracking only, not actionable yet)
2. **Phase 3 (Week 4):** Deckard routes guide-writing tasks to Tyrell + Roy
3. **Phase 3 (Week 5):** Scribe reviews + Bryan QA tests all samples
4. **Pre-Release:** Update main README + docs/README.md with all links

---

**Sign-off:** Deckard (Lead / Architect)  
**Date:** 2026-04-29
