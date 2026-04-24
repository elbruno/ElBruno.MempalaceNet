# Decision: Default Embedder → ElBruno.LocalEmbeddings

**Date:** 2026-04-24  
**By:** Bruno Capuano (via Roy, AI/Agent Integration dev)  
**Status:** Implemented (commit `a1a265f`)

---

## Context

MemPalace.NET Phase 3 initially shipped with **Ollama** as the default embedding provider (using `nomic-embed-text`). Bruno decided to switch to **ElBruno.LocalEmbeddings** (an ONNX-based local embedder) as the new default to eliminate the external Ollama service dependency and provide a true zero-config experience.

Reference: `docs/PLAN.md` "Hard Questions — Decisions" section locked this choice.

---

## Decision

**Default embedder:** `ElBruno.LocalEmbeddings` (ONNX-based, M.E.AI-compatible)  
**Default model:** `sentence-transformers/all-MiniLM-L6-v2` (384 dimensions)

---

## Package Versions

| Package | Version | Notes |
|---------|---------|-------|
| `ElBruno.LocalEmbeddings` | **1.4.3** | ONNX-based local embedder, zero external runtime deps |
| `Microsoft.Extensions.AI.Abstractions` | **10.4.1** | Upgraded from 10.3.0 (required by ElBruno package) |
| `Microsoft.Extensions.Options` | **10.0.5** | Upgraded from 9.0.1 (required by ElBruno package) |
| `Microsoft.Extensions.AI` | 9.5.0 | Unchanged |
| `Microsoft.Extensions.AI.Ollama` | 9.1.0-preview.1.25064.3 | Unchanged (opt-in) |
| `Microsoft.Extensions.AI.OpenAI` | 10.3.0 | Unchanged (opt-in, not yet implemented) |

---

## Rationale

**Why Local (ElBruno.LocalEmbeddings)?**
1. **Zero external dependencies**: No need to install or run Ollama service.
2. **Privacy-first**: Embeddings generated entirely on-device, no data leaves the machine.
3. **No API keys or costs**: Completely free, no cloud dependencies.
4. **Fast startup**: ONNX model downloads automatically on first use and caches locally.
5. **M.E.AI compatible**: Implements `IEmbeddingGenerator<string, Embedding<float>>`, plugs seamlessly into our existing abstraction.
6. **Bruno's own package**: Aligns with project authorship and trust.

**Why `all-MiniLM-L6-v2`?**
- General-purpose sentence embeddings, good balance of quality and speed.
- 384 dimensions (smaller than `all-mpnet-base-v2`'s 768, faster for local use).
- Well-tested in HuggingFace ecosystem.
- Default in ElBruno.LocalEmbeddings package.

---

## Alternative Providers (Opt-In)

- **Ollama**: Still available as opt-in provider (configure `Provider = "Ollama"` in `EmbedderOptions`). Requires Ollama service installed.
- **OpenAI / Azure OpenAI**: Planned for Phase 4 (currently stubbed, throw `NotImplementedException`).

---

## Model Cache Location

Downloaded ONNX models are cached in:
- **Windows**: `%USERPROFILE%\.cache\huggingface\hub`
- **Linux/macOS**: `~/.cache/huggingface/hub`

First run downloads the model (typically 20-100 MB). Subsequent runs load from cache instantly.

---

## ModelIdentity Convention

Format: `local:<model-name>`

Examples:
- `local:sentence-transformers/all-MiniLM-L6-v2` (default)
- `local:sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` (if custom model configured)

Stored alongside vectors in SQLite backend to enforce embedder consistency (same pattern as Python MemPalace `EmbedderIdentityMismatchError`).

---

## Testing

- Added `LocalEmbedderRegistrationTests.cs` (6 tests, all green).
- Tests verify DI configuration only (no model downloads triggered in tests).
- Existing `MeaiEmbedderTests.cs` (11 tests) still green (mock-based, no changes needed).

---

## Documentation

- Updated `docs/ai.md`: Local provider is now the hero section, with provider comparison table.
- Updated `src/MemPalace.Cli/Program.cs`: comment reflects Local as default.

---

## Next Steps

- Phase 4: End-to-end embedding pipeline (mine files → embed → store in SQLite → search).
- OpenAI/Azure provider implementations (currently stubbed).

---

## References

- NuGet: https://www.nuget.org/packages/ElBruno.LocalEmbeddings
- GitHub: https://github.com/elbruno/elbruno.localembeddings
- HuggingFace model: https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2
