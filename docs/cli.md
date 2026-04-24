# CLI Reference

The `mempalace` command-line interface provides tools for initializing, mining, searching, and managing your memory palace.

## Commands

| Command | Description | Phase |
|---------|-------------|-------|
| `mempalace init` | Initialize a new palace | Phase 4 |
| `mempalace mine` | Mine memories from files or conversations | Phase 4 |
| `mempalace search` | Search for memories | Phase 4 |
| `mempalace wake-up` | Load context summary for new session | Phase 4 |
| `mempalace agents list` | List all agents in the palace | Phase 8 |
| `mempalace kg add` | Add a relationship to the knowledge graph | Phase 6 |
| `mempalace kg query` | Query the knowledge graph | Phase 6 |
| `mempalace kg timeline` | View entity timeline | Phase 6 |

---

## `mempalace init`

Initialize a new memory palace at the specified path.

**Usage:**
```bash
mempalace init <path> [--name <name>]
```

**Arguments:**
- `<path>` - Directory where the palace will be initialized

**Options:**
- `--name` - Optional name for the palace (default: directory name)

**Examples:**
```bash
# Initialize in current directory
mempalace init .

# Initialize in a specific directory
mempalace init ./my-palace

# Initialize with a custom name
mempalace init ./my-palace --name "My Memory Palace"
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., directory already exists, invalid path)

---

## `mempalace mine`

Mine memories from files or conversations and store them in the palace.

**Usage:**
```bash
mempalace mine <path> [--mode <mode>] [--wing <wing>]
```

**Arguments:**
- `<path>` - Path to mine for memories

**Options:**
- `--mode` - Mining mode: `files` or `convos` (default: `files`)
- `--wing` - Target wing for mined content (default: auto-detect)

**Examples:**
```bash
# Mine files from a project directory
mempalace mine ./my-project

# Mine conversation transcripts
mempalace mine ~/.claude/projects --mode convos

# Mine into a specific wing
mempalace mine ./docs --mode files --wing documentation
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., invalid path, mining failure)

---

## `mempalace search`

Search for memories using semantic search.

**Usage:**
```bash
mempalace search <query> [--wing <wing>] [--rerank] [--top-k <n>]
```

**Arguments:**
- `<query>` - Search query text

**Options:**
- `--wing` - Limit search to a specific wing (default: all wings)
- `--rerank` - Enable LLM-based reranking (default: false)
- `--top-k` - Number of results to return (default: 10)

**Examples:**
```bash
# Basic search
mempalace search "vector databases"

# Search with reranking
mempalace search "CLI design patterns" --rerank

# Search in a specific wing with custom result count
mempalace search "authentication" --wing code --top-k 5
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., invalid query, search failure)

---

## `mempalace wake-up`

Load a context summary for a new session, showing recent activity and relevant memories.

**Usage:**
```bash
mempalace wake-up
```

**Examples:**
```bash
mempalace wake-up
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., palace not initialized)

---

## `mempalace agents list`

List all agents that have wings in the palace.

**Usage:**
```bash
mempalace agents list
```

**Examples:**
```bash
mempalace agents list
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., palace not initialized)

---

## `mempalace kg add`

Add a relationship to the knowledge graph.

**Usage:**
```bash
mempalace kg add <subject> <predicate> <object> [--valid-from <time>] [--valid-to <time>]
```

**Arguments:**
- `<subject>` - Subject entity
- `<predicate>` - Relationship type
- `<object>` - Object entity

**Options:**
- `--valid-from` - Validity start time (ISO 8601 format)
- `--valid-to` - Validity end time (ISO 8601 format)

**Examples:**
```bash
# Add a simple relationship
mempalace kg add Tyrell worked-on MemPalace.Core

# Add a relationship with temporal bounds
mempalace kg add Tyrell worked-on Phase1 --valid-from 2026-04-24T10:00:00 --valid-to 2026-04-24T16:00:00
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., invalid entity, invalid time format)

---

## `mempalace kg query`

Query the knowledge graph using pattern matching.

**Usage:**
```bash
mempalace kg query <pattern> [--at <time>]
```

**Arguments:**
- `<pattern>` - Query pattern (use `?` as a wildcard)

**Options:**
- `--at` - Query as of specific time (ISO 8601 format, default: current)

**Examples:**
```bash
# Find who worked on a project
mempalace kg query "? worked-on MemPalace.Core"

# Find what someone worked on
mempalace kg query "Tyrell worked-on ?"

# Query at a specific time
mempalace kg query "? worked-on ?" --at 2026-04-24T12:00:00
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., invalid pattern, invalid time format)

---

## `mempalace kg timeline`

View the timeline of events for a specific entity.

**Usage:**
```bash
mempalace kg timeline <entity> [--from <time>] [--to <time>]
```

**Arguments:**
- `<entity>` - Entity to view timeline for

**Options:**
- `--from` - Start time (ISO 8601 format)
- `--to` - End time (ISO 8601 format)

**Examples:**
```bash
# View full timeline
mempalace kg timeline Tyrell

# View timeline for a specific period
mempalace kg timeline MemPalace.Core --from 2026-04-24 --to 2026-04-25
```

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., entity not found, invalid time format)

---

## Configuration File

The CLI reads configuration from `mempalace.json` in the current directory (or the palace root directory).

**Schema:**

```json
{
  "palace": {
    "name": "My Palace",
    "path": "./my-palace"
  },
  "backend": {
    "type": "sqlite",
    "connectionString": "Data Source=palace.db"
  },
  "embedder": {
    "provider": "ollama",
    "model": "nomic-embed-text",
    "endpoint": "http://localhost:11434"
  },
  "search": {
    "defaultTopK": 10,
    "enableRerank": false
  }
}
```

---

## Environment Variables

Configuration can be overridden using environment variables with the `MEMPALACE_` prefix:

- `MEMPALACE_PALACE__NAME` - Palace name
- `MEMPALACE_PALACE__PATH` - Palace path
- `MEMPALACE_BACKEND__TYPE` - Backend type
- `MEMPALACE_EMBEDDER__PROVIDER` - Embedder provider
- `MEMPALACE_EMBEDDER__MODEL` - Embedder model
- `MEMPALACE_EMBEDDER__ENDPOINT` - Embedder endpoint

Example:
```bash
export MEMPALACE_EMBEDDER__MODEL=nomic-embed-text
mempalace search "vector databases"
```

---

## Exit Codes

All commands use the following exit code convention:

- `0` - Command completed successfully
- `1` - General error (validation, runtime error, etc.)
- `2` - Configuration error (invalid config file, missing required settings)
- `3` - Backend error (database connection, storage error)
- `4` - Embedder error (embedding service unavailable, invalid model)

---

## Phase Status

**Phase 5 (Current):** CLI scaffold with stub implementations is complete. All commands parse correctly and display placeholder UI. Full implementations will be added in subsequent phases.

**Next Steps:**
- Phase 4: Backend and search pipeline implementations
- Phase 6: Knowledge graph operations
- Phase 8: Agent management features
