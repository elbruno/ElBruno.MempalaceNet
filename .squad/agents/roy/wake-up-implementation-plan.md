# Wake-Up Command Implementation Plan

**Owner:** Roy (AI/Agent Integration)  
**Target:** v0.7.0  
**Status:** Design Complete, Awaiting Team Approval

---

## Current State Audit

### WakeUpCommand.cs Status

**Location:** `src/MemPalace.Cli/Commands/WakeUpCommand.cs`

**Current Implementation:**
```csharp
internal sealed class WakeUpCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // TODO(phase4): implementation pending
        // Shows hardcoded panel with example metadata
        return 0;
    }
}
```

**Status:** ✅ Stub exists, registered in CLI, but non-functional

**Command Registration:** ✅ Present in `Program.cs`:
```csharp
config.AddCommand<WakeUpCommand>("wake-up")
    .WithDescription("Load context summary for new session")
    .WithExample("mempalacenet wake-up");
```

**Documentation Status:**
- ✅ Listed in `docs/cli.md` (placeholder section)
- ✅ Marked as "Phase 4" in roadmap
- ⚠️ Removed from README quick start (not yet functional)

---

## Design Summary

### Architecture Decision

**Pattern:** Microsoft.Extensions.AI `IChatClient` abstraction with cloud-first default

**Key Components:**

1. **IBackend Extension:**
   - Add `WakeUpAsync(wing?, days, limit)` method
   - Returns recent memories ordered by timestamp
   - SQLite implementation with date filtering

2. **Service Layer:**
   - `IWakeUpService` interface
   - `WakeUpService` implementation
   - Dependencies: `IBackend`, `IChatClient?`, `WakeUpOptions`
   - Graceful degradation when IChatClient is null

3. **CLI Integration:**
   - Replace stub in `WakeUpCommand.cs`
   - Add settings: `--days`, `--wing`, `--limit`
   - Spectre.Console rendering (panels for metadata + summary)

4. **Configuration:**
   - `WakeUpOptions` with defaults (7 days, 100 limit)
   - Customizable system prompt
   - Enable/disable flag

### LLM Integration Strategy

**Default:** Cloud LLM (OpenAI/Azure) — user configures API key  
**Opt-In:** Local (Ollama) or custom IChatClient via DI  
**Fallback:** Metadata-only display when LLM unavailable

**M.E.AI Abstractions:**
- `IChatClient` from Microsoft.Extensions.AI.Abstractions (already in solution)
- No hard dependencies on OpenAI/Ollama SDKs
- User installs provider package as needed

---

## Implementation Phases

### Phase 1: Backend Extension (3-5 days)
**Owner:** Tyrell (with Roy consultation)

**Tasks:**
1. Add `WakeUpAsync()` to `IBackend` interface
2. Implement in `SqliteBackend` with date filtering
3. Write tests (`WakeUpAsyncTests.cs`)

**Deliverables:**
- `IBackend.WakeUpAsync(wing?, days, limit)` method
- SQLite implementation with timestamp filtering
- Unit tests (filter by days, wing, limit edge cases)

---

### Phase 2: Service Layer (5-7 days)
**Owner:** Roy

**Tasks:**
1. Create `IWakeUpService` / `WakeUpService`
2. Implement LLM prompt generation
3. Add fallback logic (no IChatClient)
4. Implement metadata calculation
5. Write tests with mocked dependencies

**Deliverables:**
- `src/MemPalace.WakeUp/` project (or add to existing)
- Service interfaces and implementation
- Prompt templates (system + user)
- Cost control (50 memory limit)
- `WakeUpServiceTests.cs` (10+ tests)

---

### Phase 3: CLI Command (2-3 days)
**Owner:** Roy + Rachael

**Tasks:**
1. Replace `WakeUpCommand.cs` stub
2. Add command settings (`--days`, `--wing`, `--limit`)
3. Implement Spectre.Console rendering
4. Wire up DI in `Program.cs`
5. Write integration tests

**Deliverables:**
- Functional `WakeUpCommand` with settings
- Spectre.Console panels (metadata + summary)
- DI registration in CLI
- Integration tests (with/without IChatClient)

---

### Phase 4: Documentation (1-2 days)
**Owner:** Roy

**Tasks:**
1. ✅ **COMPLETE:** Create `docs/guides/wake-up-summarization.md`
2. Update `docs/cli.md` with real usage examples
3. Add examples to `examples/WakeUpDemo/` (optional)

**Deliverables:**
- ✅ Comprehensive config guide (OpenAI, Azure, Ollama)
- Updated CLI reference docs
- Example code (optional)

---

## Configuration Examples

### OpenAI (Cloud Default)

**Prerequisites:**
- `Microsoft.Extensions.AI.OpenAI` NuGet package
- OPENAI_API_KEY environment variable

**Registration:**
```csharp
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
services.AddChatClient(builder => builder
    .UseOpenAI("gpt-4o-mini", openAiKey));

services.AddMemPalaceWakeUp(o =>
{
    o.DefaultDays = 7;
    o.DefaultLimit = 100;
    o.Enabled = true;
});
```

**Cost:** ~$0.001 per wake-up (gpt-4o-mini)

---

### Azure OpenAI (Enterprise)

**Prerequisites:**
- Azure OpenAI resource + deployment
- Environment variables: AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT

**Registration:**
```csharp
var azureClient = new AzureOpenAIClient(
    new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")),
    new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")));

services.AddChatClient(builder => builder
    .Use(azureClient.AsChatClient(Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT"))));
```

---

### Ollama (Local Opt-In)

**Prerequisites:**
- Ollama installed + model pulled (`ollama pull llama3.2`)
- `Microsoft.Extensions.AI.Ollama` NuGet package

**Registration:**
```csharp
services.AddChatClient(builder => builder
    .UseOllama("llama3.2", new Uri("http://localhost:11434")));
```

**Tradeoffs:**
- ✅ No API costs, privacy-first, offline
- ⚠️ Slower (5-15 seconds vs 1-3 seconds)
- ⚠️ Requires local GPU/CPU

---

## M.E.AI Integration Points

### Existing Patterns (Reuse)

1. **IEmbedder abstraction:**
   - Already wraps `IEmbeddingGenerator<string, Embedding<float>>`
   - Pluggable providers: Local, OpenAI, Azure
   - Config via `EmbedderOptions`

2. **LlmReranker stub:**
   - Already takes `IChatClient` parameter
   - Prompt logic pending (Phase 9)
   - Wake-up follows same pattern

3. **DI registration:**
   - `AddMemPalaceAi()` for embeddings
   - `AddMemPalaceWakeUp()` for wake-up (new)
   - User registers `IChatClient` separately

### New Integration Points

1. **WakeUpService → IChatClient:**
   - Calls `CompleteAsync(messages, options, ct)`
   - System prompt + user prompt (memory list)
   - Max tokens: 512 (configurable)

2. **Graceful degradation:**
   - Check if `IChatClient` is null
   - If null → return fallback summary (metadata only)
   - Log warning to stderr

3. **Cost control:**
   - Limit to 50 memories max (token budget)
   - Truncate if user requests more
   - Add warning in output if truncated

---

## Testing Strategy

### Unit Tests (WakeUpServiceTests.cs)
- ✅ Prompt generation (system + user)
- ✅ Metadata calculation (last session, wings, count)
- ✅ Fallback logic (no IChatClient)
- ✅ Token budget enforcement (50 memory limit)
- ✅ Error handling (LLM timeout, API errors)

### Integration Tests
- ✅ End-to-end: mine → store → wake-up
- ✅ With IChatClient (mocked OpenAI)
- ✅ Without IChatClient (graceful fallback)
- ✅ CLI output validation (Spectre.Console)

### Manual Smoke Tests
- OpenAI: Real API call, verify summary quality
- Azure: Enterprise endpoint, verify auth
- Ollama: Local model, verify latency
- No client: Verify fallback message + docs link

---

## Effort Estimate

| Phase | Owner | Duration | Status |
|-------|-------|----------|--------|
| Backend Extension | Tyrell | 3-5 days | 🚧 Pending |
| Service Layer | Roy | 5-7 days | 🚧 Pending |
| CLI Command | Roy + Rachael | 2-3 days | 🚧 Pending |
| Documentation | Roy | 1-2 days | ✅ Design docs complete |
| **Total** | — | **11-17 days** | **~2-3 weeks** |

**Realistic estimate:** 14 days (2.5 weeks with reviews, integration, testing)

---

## Dependencies & Blockers

### No External Dependencies
- ✅ M.E.AI.Abstractions already in solution
- ✅ No new NuGet packages required for core implementation
- ✅ User installs provider packages as needed (OpenAI, Ollama)

### Internal Dependencies
- ⚠️ **Tyrell:** IBackend.WakeUpAsync() implementation (Phase 1)
- ⚠️ **Rachael:** CLI rendering review (Phase 3)
- ⚠️ **Bruno:** Approval of cloud-default strategy

### Potential Blockers
- OpenAI API rate limits (testing)
- M.E.AI.Ollama package stability (preview status)
- Token cost concerns (mitigated by gpt-4o-mini default)

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **LLM cost** | High | Default to gpt-4o-mini (~$0.001/run), document cost clearly |
| **Token limits** | Medium | Truncate to 50 memories, prioritize recent |
| **No IChatClient** | Low | Graceful fallback, clear docs |
| **Prompt quality** | Medium | Make system prompt configurable, iterate based on feedback |
| **Latency** | Low | Show spinner, consider caching (future) |
| **Ollama instability** | Low | Cloud default, local opt-in |

---

## Success Criteria

### Functional
- ✅ `mempalacenet wake-up` works with OpenAI/Azure
- ✅ Graceful fallback when IChatClient is null
- ✅ Filters work correctly (--days, --wing, --limit)
- ✅ Spectre.Console rendering is clear and helpful

### Documentation
- ✅ Reference config guide complete (OpenAI, Azure, Ollama)
- ⚠️ CLI docs updated with real usage examples (pending)

### Testing
- ⚠️ 10+ unit tests (prompt, metadata, fallback) — pending implementation
- ⚠️ 3+ integration tests (end-to-end) — pending implementation

### Performance
- ✅ Design supports <10 seconds latency (gpt-4o-mini)
- ✅ Cost control built into design (50 memory limit, 512 token max)

---

## Next Steps

1. **Immediate:**
   - ✅ ADR created: `.squad/decisions/inbox/roy-wakeup-llm-integration.md`
   - ✅ Reference guide created: `docs/guides/wake-up-summarization.md`
   - ⏸️ Await team approval (Bruno, Deckard, Tyrell, Rachael)

2. **Post-Approval:**
   - Create GitHub issue for v0.7.0 milestone
   - Coordinate with Tyrell on IBackend.WakeUpAsync() API
   - Implement Phase 1-3 (backend → service → CLI)
   - Write tests and update CLI docs

3. **v0.7.0 Delivery:**
   - Ship v0.7.0-preview.1 with functional wake-up
   - Iterate on prompt quality based on feedback
   - Ship v0.7.0 stable

---

## References

- **ADR:** `.squad/decisions/inbox/roy-wakeup-llm-integration.md`
- **Config Guide:** `docs/guides/wake-up-summarization.md`
- **Current Stub:** `src/MemPalace.Cli/Commands/WakeUpCommand.cs`
- **Mission Brief:** User request (cloud default, local opt-in, M.E.AI abstractions)

---

**Status:** Design phase complete. Ready for team review and v0.7.0 sprint kickoff.
