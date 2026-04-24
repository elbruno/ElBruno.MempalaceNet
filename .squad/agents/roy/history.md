# Roy — History

## Core Context
- **Project:** MemPalace.NET — port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** AI / Agent integration
- **Mandate:** Use Microsoft.Extensions.AI for all LLM/embedding abstractions. Use Microsoft Agent Framework for agent layer.
- **Key NuGet packages (latest):** `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.Ollama`, `Microsoft.Extensions.AI.OpenAI`, `Microsoft.Agents.AI`, `ModelContextProtocol`.
- **Defaults:** local-first → Ollama embedder (`nomic-embed-text`) by default, OpenAI optional behind config. No telemetry without opt-in.

## Learnings

### 2026-04-24: Phase 3 — M.E.AI Integration Complete
**What:** Delivered AI integration layer via Microsoft.Extensions.AI.
- Implemented `MeaiEmbedder` adapter wrapping `IEmbeddingGenerator<string, Embedding<float>>` to MemPalace's `IEmbedder` interface (from Tyrell's Phase 1).
- Created DI registration via `AddMemPalaceAi(options)` with provider abstraction (Ollama/OpenAI/Azure).
- Default provider: **Ollama** (`nomic-embed-text` @ `localhost:11434`) — local-first, no API keys.
- Stubbed OpenAI/Azure providers (throw `NotImplementedException` for now; Phase 4 will complete).
- Added `IReranker` interface + `LlmReranker` skeleton (full LLM prompt deferred to Phase 9).
- Wrote comprehensive test suite: 11 tests (NSubstitute mocks), all green.
- Authored `docs/ai.md` with usage examples, provider switching, package versions.
- Committed and pushed (commit `24e4deb`).

**Key challenges:**
1. **Package versioning chaos**: M.E.AI packages have mismatched stable/preview versions. Ollama provider only available as `9.1.0-preview.1.25064.3`; OpenAI package at `10.3.0` but doesn't expose `AsEmbeddingGenerator` extension. Resolved by using Ollama for Phase 3, stubbing OpenAI for Phase 4.
2. **OpenAI API surface mismatch**: `OpenAIClient.AsEmbeddingGenerator(model)` doesn't exist in current M.E.AI.OpenAI package. Need to revisit when stable APIs emerge or wrap `EmbeddingClient` manually.
3. **CLI test failures**: Pre-existing broken tests in `MemPalace.Tests/Cli/CommandAppParseTests.cs` (internal accessibility errors). Not in scope for Phase 3, left for Rachael (Phase 5 owner) to fix.

**Learnings:**
- M.E.AI abstractions are clean but ecosystem is still preview-heavy (Ollama especially).
- Dimension inference from first embedding call works cleanly; matches Python MemPalace pattern.
- DI factory pattern (switch on provider string) keeps registration simple.
- Testing with NSubstitute for `IEmbeddingGenerator` is straightforward; no real Ollama needed in tests.

**Next up (Phase 4 — Tyrell + Roy):** End-to-end embedding pipeline: mine files → embed → store in SQLite → search. Will need to wire `MeaiEmbedder` into mining and make OpenAI/Azure providers functional.
