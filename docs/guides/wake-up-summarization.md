# Wake-Up Summarization Guide

**Feature:** Conversational context summaries for starting new sessions  
**Status:** v0.7.0 (preview)  
**Dependencies:** Microsoft.Extensions.AI.Abstractions (included), ElBruno.LocalLLMs (included)

---

## Overview

The `mempalacenet wake-up` command generates a **natural language summary** of your recent memory palace activity using an LLM. This helps you quickly recall context when resuming work after a break.

**What it does:**
1. Fetches recent memories from your palace (last N days, configurable)
2. Sends them to an LLM for summarization (local-first by default)
3. Displays a formatted summary with metadata (last session, memory count, active wings)

**Design philosophy:**
- **Local-first default:** Qwen2.5-0.5B (via ElBruno.LocalLLMs) — zero config, no API keys
- **Cloud opt-in:** OpenAI/Azure via environment variables for faster/better summaries
- **Graceful degradation:** Works without LLM (shows metadata only)
- **Pluggable:** Leverage Microsoft.Extensions.AI abstractions (no hard SDK dependencies)

---

## Quick Start

### 1. Run Wake-Up (Local LLM — First Time)

**First run automatically downloads Qwen2.5-0.5B (~500 MB):**

```bash
mempalacenet wake-up
```

**Output (first run):**
```
[INFO] Downloading model: Qwen2.5-0.5B-Instruct (~500 MB)
[INFO] Progress: 25%... 50%... 75%... 100%
[INFO] Model cached at ~/.cache/mempalace/models/qwen2.5-0.5b-instruct
```

**Output (subsequent runs):**
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
╰─────────────────────────────────────────────────────╯
```

**Performance (local):**
- First run: ~1 minute (model download)
- Subsequent runs: 5-10 seconds (CPU) or 2-3 seconds (GPU)

### 2. Cloud Opt-In (OpenAI — Faster/Better)

If you prefer cloud LLMs (faster, higher quality), set your API key:

```bash
export OPENAI_API_KEY="sk-..."
mempalacenet wake-up
```

**Performance (cloud):**
- Response time: 1-3 seconds
- Cost: ~$0.001 per run (gpt-4o-mini)

---

## Configuration Examples

### Local-First (Default — Zero Config)

**No setup required!** The first `wake-up` command automatically:
1. Downloads Qwen2.5-0.5B-Instruct (~500 MB) from HuggingFace
2. Caches the model locally in `~/.cache/mempalace/models/`
3. Uses CPU inference (or GPU if CUDA/DirectML is available)

**Requirements:**
- ~500 MB disk space for model
- 2 GB RAM minimum
- CPU with AVX2 support (most modern CPUs)

**Optional GPU acceleration:**
- NVIDIA GPU: Install `Microsoft.ML.OnnxRuntimeGenAI.Cuda` NuGet package
- Any Windows GPU: Install `Microsoft.ML.OnnxRuntimeGenAI.DirectML` NuGet package

**Model caching:**
Models are cached at:
- **Windows:** `%USERPROFILE%\.cache\mempalace\models`
- **Linux/macOS:** `~/.cache/mempalace/models`

To clear cache (force re-download):
```bash
rm -rf ~/.cache/mempalace/models
```

**Prerequisites:**
- OpenAI API key (https://platform.openai.com/api-keys)

**Setup (override default local LLM):**
```bash
export OPENAI_API_KEY="sk-..."
mempalacenet wake-up
```

**Or configure via appsettings.json:**
```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o-mini"
  }
}
```

**Cost estimate:**
- **gpt-4o-mini:** ~$0.15 per 1M input tokens, ~$0.60 per 1M output tokens
- Typical wake-up: 100 memories × 50 tokens = 5K tokens input, 500 tokens output = **$0.001 per run**
- **gpt-4o:** ~$2.50/1M input, ~$10/1M output = **$0.015 per run**

**Recommended model:** `gpt-4o-mini` (best balance of cost and quality)

---

### Azure OpenAI (Enterprise — Opt-In)

**Prerequisites:**
- Azure OpenAI resource (https://portal.azure.com)
- Deployed model (e.g., `gpt-4o-mini`, `gpt-35-turbo`)
- Endpoint URL and API key

**Setup (override default local LLM):**
```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="..."
export AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
mempalacenet wake-up
```

**Or configure via appsettings.json:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "...",
    "Deployment": "gpt-4o-mini"
  }
}
```

---

### Ollama (Local — Alternative)

**Note:** Ollama is not directly integrated in v0.7.0. To use Ollama, you must:
1. Manually register an `IChatClient` using `Microsoft.Extensions.AI.Ollama` (preview)
2. Override the default ElBruno.LocalLLMs registration in `Program.cs`

**Example (not officially supported):**
```csharp
// In Program.cs, replace default IChatClient registration
services.AddSingleton<IChatClient>(sp =>
{
    return new OllamaClient(new Uri("http://localhost:11434"), "llama3.2");
});
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

### Error: "Failed to initialize local LLM"

**Cause:** Model download failed or system requirements not met.

**Fix:**
1. Check internet connection (model downloads from HuggingFace)
2. Verify disk space: ~500 MB required in `~/.cache/mempalace/models`
3. Check CPU support: Requires AVX2 (most CPUs since 2013)
4. Use cloud opt-in as fallback:
   ```bash
   export OPENAI_API_KEY="sk-..."
   mempalacenet wake-up
   ```

---

### Slow performance (local LLM)

**Cause:** CPU inference is slow on older hardware.

**Fix:**
1. **GPU acceleration (NVIDIA):**
   ```bash
   dotnet add package Microsoft.ML.OnnxRuntimeGenAI.Cuda
   ```
2. **GPU acceleration (Windows — any GPU):**
   ```bash
   dotnet add package Microsoft.ML.OnnxRuntimeGenAI.DirectML
   ```
3. **Cloud fallback:**
   ```bash
   export OPENAI_API_KEY="sk-..."
   mempalacenet wake-up
   ```

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

### Local LLM (Default — Free)

**Cost:** $0 (no API charges, no subscription)

**Infrastructure:**
- Local CPU (default): Any modern CPU with AVX2 support
- Local GPU (optional): NVIDIA RTX 3060 or better (~$300-500) or DirectML (Windows GPU)

**Latency:**
- CPU: 5-10 seconds
- GPU: 2-3 seconds

**First-time setup:**
- Model download: ~1 minute (~500 MB)
- Cached locally, no re-download needed

### OpenAI Pricing (Cloud Opt-In)

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

**Recommendation:** Use local LLM (default, free) for most use cases. Use `gpt-4o-mini` (cloud) only if you need faster response times or higher quality summaries.

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
