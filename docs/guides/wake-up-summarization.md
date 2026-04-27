# Wake-Up Summarization Guide

**Feature:** Conversational context summaries for starting new sessions  
**Status:** v0.7.0 (preview)  
**Dependencies:** Microsoft.Extensions.AI.Abstractions (included), user-provided IChatClient

---

## Overview

The `mempalacenet wake-up` command generates a **natural language summary** of your recent memory palace activity using an LLM. This helps you quickly recall context when resuming work after a break.

**What it does:**
1. Fetches recent memories from your palace (last N days, configurable)
2. Sends them to an LLM (OpenAI, Azure OpenAI, Ollama, or custom) for summarization
3. Displays a formatted summary with metadata (last session, memory count, active wings)

**Design philosophy:**
- **Cloud-first default:** OpenAI/Azure (user configures API key)
- **Local opt-in:** Ollama or custom LLM via `IChatClient` DI
- **Graceful degradation:** Works without LLM (shows metadata only)
- **Pluggable:** Leverage Microsoft.Extensions.AI abstractions (no hard SDK dependencies)

---

## Quick Start

### 1. Run Wake-Up (Metadata Only)

Without LLM configuration, wake-up shows basic metadata:

```bash
mempalacenet wake-up
```

**Output:**
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
           mempalacenet wake-up
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

╭─ Session Context ──────────────────────────────────╮
│ Last session: 2026-04-23 16:45 (1.2 days ago)     │
│ Memory count: 47                                    │
│ Active wings: code, conversations, docs             │
╰─────────────────────────────────────────────────────╯

╭─ Summary ───────────────────────────────────────────╮
│ Recent Activity Summary (LLM not configured):       │
│ - Last session: 2026-04-23 16:45 (1.2 days ago)    │
│ - Memory count: 47                                  │
│ - Active wings: code, conversations, docs           │
│                                                      │
│ To enable AI-powered summaries, register an         │
│ IChatClient via DI. See docs/guides/                │
│ wake-up-summarization.md for configuration examples.│
╰─────────────────────────────────────────────────────╯
```

### 2. Configure LLM (OpenAI Example)

**Install NuGet package:**
```bash
dotnet add package Microsoft.Extensions.AI.OpenAI
```

**Register IChatClient in Program.cs:**
```csharp
using Microsoft.Extensions.AI;

// In Program.cs (before building ServiceCollection)
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

**Run wake-up:**
```bash
export OPENAI_API_KEY="sk-..."
mempalacenet wake-up
```

**Output (with LLM):**
```
╭─ Summary ───────────────────────────────────────────╮
│ Recent Activity Summary:                             │
│                                                      │
│ Key themes from the last 7 days:                    │
│ • Implemented BM25 keyword search in hybrid search  │
│ • Completed temporal knowledge graph (Phase 6)      │
│ • Added MCP server with 7 tools for Claude Desktop │
│ • Benchmarked vector search performance             │
│                                                      │
│ Active projects:                                     │
│ • MemPalace.Search (BM25 + hybrid search)           │
│ • MemPalace.KnowledgeGraph (temporal triples)       │
│ • MemPalace.Mcp (Model Context Protocol server)     │
│                                                      │
│ Recent decisions:                                    │
│ • Switched from Ollama to LocalEmbeddings (ONNX)    │
│ • Custom BM25 implementation (no external deps)     │
│ • RRF fusion for hybrid search (v0.6.0)             │
╰─────────────────────────────────────────────────────╯
```

---

## Configuration Examples

### OpenAI (Cloud)

**Prerequisites:**
- OpenAI API key (https://platform.openai.com/api-keys)
- `Microsoft.Extensions.AI.OpenAI` NuGet package

**Registration:**
```csharp
using Microsoft.Extensions.AI;

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(openAiKey))
{
    throw new InvalidOperationException(
        "OPENAI_API_KEY environment variable not set. " +
        "Get your API key at https://platform.openai.com/api-keys");
}

services.AddChatClient(builder => builder
    .UseOpenAI("gpt-4o-mini", openAiKey)); // Or "gpt-4o", "gpt-4-turbo"
```

**Cost estimate:**
- **gpt-4o-mini:** ~$0.15 per 1M input tokens, ~$0.60 per 1M output tokens
- Typical wake-up: 100 memories × 50 tokens = 5K tokens input, 500 tokens output = **$0.001 per run**
- **gpt-4o:** ~$2.50/1M input, ~$10/1M output = **$0.015 per run**

**Recommended model:** `gpt-4o-mini` (best balance of cost and quality)

---

### Azure OpenAI (Enterprise)

**Prerequisites:**
- Azure OpenAI resource (https://portal.azure.com)
- Deployed model (e.g., `gpt-4o-mini`, `gpt-35-turbo`)
- Endpoint URL and API key

**Registration:**
```csharp
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var azureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

if (string.IsNullOrEmpty(azureEndpoint) || 
    string.IsNullOrEmpty(azureKey) || 
    string.IsNullOrEmpty(deploymentName))
{
    throw new InvalidOperationException(
        "Azure OpenAI environment variables not set. Required: " +
        "AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT");
}

var azureClient = new AzureOpenAIClient(
    new Uri(azureEndpoint),
    new AzureKeyCredential(azureKey));

services.AddChatClient(builder => builder
    .Use(azureClient.AsChatClient(deploymentName)));
```

**Environment variables example:**
```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="..."
export AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
```

---

### Ollama (Local)

**Prerequisites:**
- Ollama installed (https://ollama.ai)
- Model pulled: `ollama pull llama3.2` (or `mistral`, `phi3`, etc.)
- `Microsoft.Extensions.AI.Ollama` NuGet package (preview)

**Registration:**
```csharp
using Microsoft.Extensions.AI;

services.AddChatClient(builder => builder
    .UseOllama("llama3.2", new Uri("http://localhost:11434")));
```

**Recommended models:**
- **llama3.2** (3B): Fast, good quality, 2GB RAM
- **mistral** (7B): Excellent balance, 4GB RAM
- **llama3.1** (8B): High quality, 5GB RAM

**Tradeoffs:**
- **Pros:** No API costs, privacy-first, offline support
- **Cons:** Slower than cloud APIs (5-15 seconds vs 1-3 seconds), requires local GPU/CPU

**Pull model:**
```bash
ollama pull llama3.2
```

**Test model:**
```bash
ollama run llama3.2 "Summarize recent AI developments"
```

---

### Custom IChatClient

You can register any custom `IChatClient` implementation:

```csharp
using Microsoft.Extensions.AI;

public class CustomChatClient : IChatClient
{
    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Your custom LLM integration (e.g., Anthropic Claude API, local model)
        var userMessage = messages.Last(m => m.Role == ChatRole.User).Text;
        var summary = await YourLlmService.GenerateSummary(userMessage);
        
        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, summary));
    }

    // Implement other IChatClient members...
}

// Register
services.AddSingleton<IChatClient, CustomChatClient>();
```

---

## Command Options

### Basic Usage

```bash
mempalacenet wake-up [--days <N>] [--wing <wing>] [--limit <N>]
```

### Options

| Flag | Description | Default |
|------|-------------|---------|
| `--days <N>` | Number of days to look back | 7 |
| `--wing <wing>` | Filter by wing (optional) | All wings |
| `--limit <N>` | Maximum number of memories to fetch | 100 |

### Examples

**Default (last 7 days, all wings):**
```bash
mempalacenet wake-up
```

**Last 14 days:**
```bash
mempalacenet wake-up --days 14
```

**Filter by wing:**
```bash
mempalacenet wake-up --wing conversations
```

**Combine options:**
```bash
mempalacenet wake-up --days 30 --wing code --limit 50
```

---

## Customizing the System Prompt

The wake-up service uses a default system prompt, but you can customize it:

**In Program.cs:**
```csharp
services.AddMemPalaceWakeUp(o =>
{
    o.SystemPrompt = @"You are a senior software engineer reviewing recent project activity.
Given a list of memories, generate a concise summary (3-5 paragraphs) highlighting:
- Critical decisions or architectural changes
- Open questions or blockers
- Next steps or priorities

Use technical language. Be specific.";
});
```

**Default prompt (from ADR):**
```
You are a memory assistant. Given a list of recent memories from a user's MemPalace, 
generate a concise context summary (3-5 paragraphs) highlighting:
- Key topics or themes
- Recent activities or decisions
- Important entities (people, projects, concepts)
- Temporal patterns (e.g., "focused on X last week, then shifted to Y")

Be specific but brief. Use bullet points for clarity. Avoid generic statements.
```

---

## Disabling Wake-Up Summarization

If you want to disable LLM summarization (e.g., for cost control):

```csharp
services.AddMemPalaceWakeUp(o =>
{
    o.Enabled = false; // Always use fallback (metadata only)
});
```

Or unregister `IChatClient` entirely (service will auto-detect and fall back).

---

## Troubleshooting

### Error: "No chat client configured"

**Cause:** `IChatClient` not registered in DI.

**Fix:** Register a chat client before calling `AddMemPalaceWakeUp()`:
```csharp
services.AddChatClient(builder => builder.UseOpenAI("gpt-4o-mini", apiKey));
services.AddMemPalaceWakeUp();
```

---

### Error: "401 Unauthorized" (OpenAI)

**Cause:** Invalid API key.

**Fix:** Check your API key:
```bash
echo $OPENAI_API_KEY
```

Verify it's active at https://platform.openai.com/api-keys.

---

### Error: "Ollama connection refused"

**Cause:** Ollama service not running.

**Fix:** Start Ollama:
```bash
ollama serve
```

Verify it's running:
```bash
curl http://localhost:11434/api/tags
```

---

### Slow performance (Ollama)

**Cause:** CPU inference is slow; model size too large.

**Fix:**
1. Use a smaller model: `ollama pull llama3.2` (3B instead of 8B)
2. Enable GPU acceleration (if available): https://ollama.ai/docs/gpu
3. Switch to cloud API for faster responses

---

### Generic summaries (low quality)

**Cause:** Prompt may not be specific enough for your use case.

**Fix:** Customize the system prompt (see "Customizing the System Prompt" above).

---

## Architecture Notes

### How It Works

1. **WakeUpCommand** calls `IWakeUpService.SummarizeAsync()`
2. **WakeUpService** fetches memories via `IBackend.WakeUpAsync(wing, days, limit)`
3. Service builds user prompt: timestamp + wing + content (tab-separated for token efficiency)
4. Service calls `IChatClient.CompleteAsync()` with system + user prompt
5. LLM returns summary (3-5 paragraphs, ~500 tokens)
6. Service returns `WakeUpSummary(summary, metadata)`
7. Command renders output via Spectre.Console

**If IChatClient is null:** Service returns fallback summary (metadata only, no LLM call).

### Prompt Format

**System prompt:** Defines assistant role and task (see "Customizing the System Prompt").

**User prompt:**
```
Here are the 47 most recent memories from the last 7 days:

2026-04-24T14:32:00Z	code	Implemented BM25 scoring in HybridSearchService
2026-04-24T10:15:00Z	docs	Updated CHANGELOG with Phase 6 deliverables
2026-04-23T16:45:00Z	conversations	Discussed KG schema with Tyrell
...

Generate a context summary for starting a new session.
```

**Token budget:**
- Typical 100 memories × 50 tokens = 5K tokens (input)
- Summary response = 500 tokens (output)
- Total = 5.5K tokens (~$0.001 for gpt-4o-mini)

**Truncation:** If memory list exceeds 50 items, only first 50 are sent to LLM (to avoid token limits).

---

## Cost Estimates

### OpenAI Pricing (as of 2026-04-25)

| Model | Input (per 1M tokens) | Output (per 1M tokens) | Wake-Up Cost |
|-------|----------------------|------------------------|--------------|
| gpt-4o-mini | $0.15 | $0.60 | **$0.001** |
| gpt-4o | $2.50 | $10.00 | $0.015 |
| gpt-4-turbo | $10.00 | $30.00 | $0.065 |

**Assumptions:**
- 100 memories
- 50 tokens per memory = 5K input tokens
- 500 tokens output
- 1 run per day = **$0.30/month** (gpt-4o-mini) or **$0.45/month** (gpt-4o)

**Recommendation:** Use `gpt-4o-mini` for production (excellent quality, 15x cheaper than gpt-4o).

### Ollama (Local)

**Cost:** $0 (no API charges)

**Infrastructure:**
- Local GPU (recommended): NVIDIA RTX 3060 or better (~$300-500)
- Local CPU (slower): Any modern CPU with 8GB+ RAM

**Latency:**
- GPU: 2-5 seconds
- CPU: 5-15 seconds

---

## Examples

### Example 1: Daily Standup Summary

**Use case:** Generate a quick recap for team standup.

**Command:**
```bash
mempalacenet wake-up --days 1 --wing work
```

**Output:**
```
Recent Activity Summary:
- Completed BM25 implementation (HybridSearchService)
- Ran LongMemEval benchmarks (R@5 improved by 8%)
- Reviewed Tyrell's PR #42 (SQLite schema upgrade)
- Updated docs/search.md with BM25 explanation

Next steps:
- Merge BM25 PR to main
- Start v0.7.0 planning (wake-up feature)
- Benchmark hybrid search vs pure vector
```

---

### Example 2: Resume After Vacation

**Use case:** Recall context after 2-week break.

**Command:**
```bash
mempalacenet wake-up --days 14 --limit 200
```

**Output:**
```
Recent Activity Summary:

Major themes:
• Phase 6 (Knowledge Graph) completed Apr 20
• Phase 7 (MCP Server) delivered Apr 22
• v0.6.0 planning in progress (BM25, sqlite-vec)

Key decisions:
• Switched from Ollama to LocalEmbeddings (ONNX) for default embedder
• Custom BM25 implementation (no external library dependencies)
• RRF fusion for hybrid search (deferred weighted fusion to v0.7+)

Active conversations:
• Discussed benchmark harness architecture with Bryant (Apr 18)
• Tyrell's MCP SSE transport proposal (deferred to v0.8.0)
• Deckard's roadmap audit flagged doc accuracy issues (fixed Apr 24)

Pending work:
• LongMemEval R@5 validation (Bryant)
• BM25 integration PR (Roy)
• Skill marketplace documentation (Deckard)
```

---

### Example 3: Wing-Specific Summary

**Use case:** Focus on a specific area (e.g., `conversations` wing for agent memory).

**Command:**
```bash
mempalacenet wake-up --wing conversations --days 7
```

**Output:**
```
Recent Conversations Summary:

Discussion topics:
• BM25 research (Roy, Apr 23-24)
• Temporal KG schema design (Tyrell, Apr 20)
• MCP tool surface decisions (Roy + Deckard, Apr 22)
• v0.7.0 scope planning (team, Apr 25)

Decisions:
• wake-up command: cloud LLM default, local opt-in
• Ollama support deferred to v0.7.0 (stable release blocker)
• Benchmark dataset: LongMemEval R@5 target

Open questions:
• Reranker prompt design (Phase 9)
• MCP write operations policy (Phase 8 follow-up)
```

---

## See Also

- **CLI Reference:** [docs/cli.md](./cli.md) — Command options and examples
- **AI Integration:** [docs/ai.md](./ai.md) — Embedder and LLM configuration
- **Architecture:** [docs/architecture.md](./architecture.md) — System design
- **Microsoft.Extensions.AI Docs:** https://learn.microsoft.com/dotnet/ai/

---

## Feedback

Have suggestions for improving wake-up summaries? Open an issue:
https://github.com/elbruno/mempalacenet/issues
