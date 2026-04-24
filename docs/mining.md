# Mining

MemPalace.NET mining infrastructure extracts content from various sources and transforms it into embedded, searchable memories.

## Overview

The mining pipeline consists of three main components:

1. **Miners** (`IMiner`) — Extract raw items from data sources
2. **MiningPipeline** — Orchestrates embedding and storage
3. **MinedItem** — Standardized representation of extracted content

## Built-in Miners

### FileSystemMiner

Recursively mines a directory for text files.

**Features:**
- Respects `.gitignore` patterns automatically
- Chunks large files (configurable size and overlap)
- Skips binary files by extension and null-byte detection
- Generates stable IDs using SHA-256 prefix for de-duplication
- Extracts metadata: path, extension, size, mtime, chunk info

**Options:**
```csharp
var ctx = new MinerContext(
    SourcePath: "/path/to/project",
    Wing: "code",
    Options: new Dictionary<string, string?>
    {
        ["chunk_size"] = "2000",     // chars per chunk (default: 2000)
        ["overlap"] = "200",          // overlap between chunks (default: 200)
        ["include"] = "**/*.cs",      // glob include pattern
        ["exclude"] = "**/bin/**"     // glob exclude pattern
    });
```

**Chunking Behavior:**
- Files ≤ `chunk_size`: Single `MinedItem`
- Files > `chunk_size`: Multiple `MinedItem`s with `chunk_index`, `chunk_start`, `chunk_end` metadata
- Overlap preserves context across chunk boundaries

### ConversationMiner

Parses conversation transcripts from LLM interactions.

**Supported Formats:**
1. **JSON Lines (.jsonl, .ndjson):**
   ```json
   {"role": "user", "content": "Hello", "timestamp": "2026-04-24T10:00:00Z"}
   {"role": "assistant", "content": "Hi there"}
   ```
   Expected fields: `role`, `content` (or `message`), optional `timestamp`

2. **Markdown:**
   ```markdown
   ## User
   What is the meaning of life?

   ## Assistant
   42.
   ```
   Supports headers: `User`, `Assistant`, `Human`, `AI` (case-insensitive)

**Metadata:**
- `role`: speaker (user/assistant/human/ai)
- `turn_index`: 0-based turn number
- `conversation_id`: derived from filename
- `timestamp`: ISO 8601 (JSONL only)

**Error Handling:**
- Invalid JSON lines are silently skipped
- Empty turns are filtered out
- Robust regex-based markdown parsing

## Mining Pipeline

### Basic Usage

```csharp
using MemPalace.Mining;

var backend = ...; // IBackend instance
var embedder = ...; // IEmbedder instance
var miner = new FileSystemMiner();

var ctx = new MinerContext(
    SourcePath: "./my-docs",
    Wing: "documentation",
    Options: new Dictionary<string, string?>());

var pipeline = new MiningPipeline();
var report = await pipeline.RunAsync(
    miner: miner,
    ctx: ctx,
    backend: backend,
    embedder: embedder,
    collection: "my-collection"
);

Console.WriteLine($"Mined: {report.ItemsMined}, Upserted: {report.Upserted}, Elapsed: {report.Elapsed}");
```

### MiningReport

```csharp
public sealed record MiningReport(
    long ItemsMined,    // Total items extracted by miner
    int Batches,        // Number of embedding batches processed
    long Embedded,      // Total items embedded
    long Upserted,      // Total items written to backend
    long Skipped,       // Duplicates within this run
    IReadOnlyList<string> Errors,  // Non-fatal errors
    TimeSpan Elapsed    // Total run time
);
```

### Batching

The pipeline groups items into batches for efficient embedding:
- Default batch size: 32
- Configurable via `batch_size` option: `ctx.Options["batch_size"] = "64"`
- Final partial batch is always processed

### De-duplication

Within a single run:
- Items with identical IDs are skipped (increments `Skipped`)
- First occurrence wins
- IDs are stable across runs (based on content hash + metadata)

Across runs:
- Backend handles idempotency via `UpsertAsync`
- Re-mining same content updates existing records

### Error Tolerance

- Batch-level errors are caught and logged to `report.Errors`
- Mining continues after batch failures
- Check `report.Errors.Count` to detect issues

## Custom Miners

Implement `IMiner`:

```csharp
public class RssFeedMiner : IMiner
{
    public string Name => "rss";

    public async IAsyncEnumerable<MinedItem> MineAsync(
        MinerContext ctx,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var feedUrl = ctx.SourcePath;
        var feed = await LoadFeedAsync(feedUrl, ct);

        foreach (var entry in feed.Entries)
        {
            ct.ThrowIfCancellationRequested();

            yield return new MinedItem(
                Id: $"rss:{feed.Id}:{entry.Id}",
                Content: $"{entry.Title}\n\n{entry.Summary}",
                Metadata: new Dictionary<string, object?>
                {
                    ["feed_title"] = feed.Title,
                    ["entry_url"] = entry.Link,
                    ["published"] = entry.PublishDate.ToString("O"),
                    ["author"] = entry.Author
                });
        }
    }
}
```

**Best Practices:**
- Generate stable IDs (content-based when possible)
- Include rich metadata for filtering/display
- Respect `CancellationToken`
- Stream items with `IAsyncEnumerable` (don't buffer)
- Parse `ctx.Options` for configuration

## DI Registration

```csharp
services.AddMemPalaceMining();  // Registers FileSystemMiner, ConversationMiner, MiningPipeline

// Custom miners (keyed services)
services.AddKeyedSingleton<IMiner, RssFeedMiner>("rss");
```

Access miners:
```csharp
var filesystemMiner = serviceProvider.GetKeyedService<IMiner>("filesystem");
var conversationMiner = serviceProvider.GetKeyedService<IMiner>("conversation");
var customMiner = serviceProvider.GetKeyedService<IMiner>("rss");
```

## CLI Integration

```bash
# Mine a codebase
mempalacenet mine ./my-project --mode files --wing code

# Mine conversation logs
mempalacenet mine ~/.claude/projects --mode convos --wing conversations

# Custom options (via config file)
{
  "mining": {
    "chunk_size": 3000,
    "overlap": 300,
    "batch_size": 64
  }
}
```

## Performance Considerations

- **FileSystemMiner:**
  - Large directories: ~1000 files/sec (depends on I/O)
  - Chunking overhead: minimal (string slicing)
  - `.gitignore` parsing: once per run

- **ConversationMiner:**
  - JSONL: streaming parser (low memory)
  - Markdown: regex-based (entire file in memory)

- **MiningPipeline:**
  - Batch size trades latency for throughput
  - Larger batches: better GPU utilization (embedder-dependent)
  - De-dupe overhead: O(n) hash set

## Roadmap

- **Additional miners:** Git commits, Slack exports, Notion backups
- **Incremental mining:** Track mtime, skip unchanged files
- **Parallel mining:** Run multiple miners concurrently
- **Schema validation:** Enforce metadata schemas per collection
