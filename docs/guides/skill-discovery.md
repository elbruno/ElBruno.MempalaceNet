# Skill Discovery & Marketplace (Phase 3 MVP)

**Version:** v0.7.0  
**Status:** ✅ Phase 3 MVP Complete  
**Last Updated:** 2026-04-28

---

## Overview

MemPalace.NET v0.7.0 introduces the **Skill Marketplace MVP**, enabling discovery and installation of reusable skills. The Phase 3 MVP focuses on local discovery with built-in demo skills; remote registry (v1.0) is deferred to future versions.

**Key Features:**
- 🔍 **Discover** available skills by name or tag
- 📋 **List** installed skills with status (enabled/disabled)
- ℹ️  **View** detailed skill information
- 📦 **Install** skills from local paths
- ✅/❌ **Enable/disable** skills without uninstalling
- 🗑️  **Uninstall** skills cleanly

---

## Quick Start

### 1. Discover Available Skills

```bash
mempalacenet skill discover
```

**Output:**
```
┌────────────────────────────┬──────────────────────┬─────────┬───────────┬────────────────────────────┐
│ ID                         │ Name                 │ Version │ Status    │ Tags                       │
├────────────────────────────┼──────────────────────┼─────────┼───────────┼────────────────────────────┤
│ rag-context-injector       │ RAG Context Injector │ 1.0.0   │ Available │ rag, semantic-search...    │
│ agent-diary                │ Agent Diary...       │ 2.1.0   │ Available │ agents, persistence...     │
│ kg-temporal-queries        │ Knowledge Graph...   │ 0.8.0   │ Available │ knowledge-graph, temporal  │
│ hybrid-search-reranking    │ Hybrid Search...     │ 1.5.0   │ Available │ search, hybrid, reranking  │
└────────────────────────────┴──────────────────────┴─────────┴───────────┴────────────────────────────┘
```

### 2. Filter by Tag

```bash
mempalacenet skill discover --tag rag
```

Shows skills tagged with `rag`.

### 3. Get Skill Details

```bash
mempalacenet skill info rag-context-injector
```

**Output:**
```
╔═══════════════════════════════════════════════════════════════╗
║                 RAG Context Injector                          ║
╠═══════════════════════════════════════════════════════════════╣
║ ID:          rag-context-injector                             ║
║ Version:     1.0.0                                            ║
║ Author:      Bruno Capuano                                    ║
║ License:     MIT                                              ║
║ Status:      ✅ Available                                     ║
║ Repository:  https://github.com/elbruno/mempalacenet-skills ║
╠═══════════════════════════════════════════════════════════════╣
║ Description:                                                  ║
║ Semantic search + LLM context injection for RAG workflows    ║
║                                                               ║
║ Dependencies:                                                 ║
║   • mempalacenet >= 0.7.0                                    ║
║   • Microsoft.Extensions.AI >= 10.3.0                        ║
║                                                               ║
║ Tags: rag, semantic-search, llm, context                     ║
╚═══════════════════════════════════════════════════════════════╝
```

### 4. Install a Skill

For v0.7.0 MVP, skills are installed from local paths (remote registry coming in v1.0):

```bash
# Clone or download a skill from GitHub
git clone https://github.com/elbruno/mempalacenet-skills.git

# Install from local path
mempalacenet skill install ./mempalacenet-skills/rag-context-injector
```

### 5. List Installed Skills

```bash
mempalacenet skill list
```

**Output:**
```
┌────────────────────────┬──────────────────┬─────────┬───────────────────┬──────────────────────┐
│ ID                     │ Name             │ Version │ Status            │ Description          │
├────────────────────────┼──────────────────┼─────────┼───────────────────┼──────────────────────┤
│ rag-context-injector   │ RAG Context...   │ 1.0.0   │ ✅ Enabled        │ Semantic search...   │
│ agent-diary            │ Agent Diary...   │ 2.1.0   │ ✅ Enabled        │ Persistent agent...  │
│ kg-temporal-queries    │ Knowledge Graph  │ 0.8.0   │ ⚠️ Disabled       │ Temporal queries...  │
└────────────────────────┴──────────────────┴─────────┴───────────────────┴──────────────────────┘
```

### 6. Enable/Disable Skills

```bash
# Enable a skill
mempalacenet skill enable kg-temporal-queries

# Disable a skill
mempalacenet skill disable rag-context-injector
```

### 7. Uninstall a Skill

```bash
mempalacenet skill uninstall rag-context-injector
```

---

## Command Reference

### `mempalacenet skill discover [OPTIONS]`

**Description:** Display available skills from the local registry.

**Options:**
| Option | Description | Default |
|--------|-------------|---------|
| `--tag <tag>` | Filter by tag (case-insensitive) | (none) |
| `--limit <n>` | Maximum number of results | 10 |

**Examples:**
```bash
# Show all available skills
mempalacenet skill discover

# Show skills with 'rag' tag
mempalacenet skill discover --tag rag

# Show first 20 skills
mempalacenet skill discover --limit 20
```

---

### `mempalacenet skill list [OPTIONS]`

**Description:** List installed skills.

**Options:**
| Option | Description |
|--------|-------------|
| `--available` | Show available + installed skills (union) |
| `--installed` | Show only installed skills (default) |
| `--enabled` | Show only enabled skills |
| `--disabled` | Show only disabled skills |

**Examples:**
```bash
# List all installed skills (default)
mempalacenet skill list

# List only enabled skills
mempalacenet skill list --enabled

# Show all discoverable skills (local + registry)
mempalacenet skill list --available
```

---

### `mempalacenet skill info <skill-id>`

**Description:** Display detailed information about a skill.

**Examples:**
```bash
mempalacenet skill info rag-context-injector
```

---

### `mempalacenet skill install <path>`

**Description:** Install a skill from a local path.

**Examples:**
```bash
# Install from current directory
mempalacenet skill install ./my-skill

# Install from absolute path
mempalacenet skill install ~/Downloads/rag-skill

# Install from cloned repository
mempalacenet skill install ./mempalacenet-skills/rag-context-injector
```

---

### `mempalacenet skill enable|disable <skill-id>`

**Description:** Enable or disable a skill.

**Examples:**
```bash
mempalacenet skill enable my-skill
mempalacenet skill disable my-skill
```

---

### `mempalacenet skill uninstall <skill-id>`

**Description:** Remove an installed skill.

**Examples:**
```bash
mempalacenet skill uninstall my-skill
```

---

## Available Skills (v0.7.0 MVP)

### 1. RAG Context Injector
- **ID:** `rag-context-injector`
- **Version:** 1.0.0
- **Tags:** rag, semantic-search, llm, context
- **Description:** Semantic search + LLM context injection for RAG workflows
- **Entry Point:** `src/run.ps1`
- **Dependencies:** mempalacenet >=0.7.0, Microsoft.Extensions.AI >=10.3.0

### 2. Agent Diary Persistence
- **ID:** `agent-diary`
- **Version:** 2.1.0
- **Tags:** agents, persistence, memory
- **Description:** Persistent agent state across conversation sessions
- **Entry Point:** `src/diary.ps1`
- **Dependencies:** mempalacenet >=0.7.0

### 3. Knowledge Graph Temporal Queries
- **ID:** `kg-temporal-queries`
- **Version:** 0.8.0
- **Tags:** knowledge-graph, temporal, queries
- **Description:** Query knowledge graph relationships across time
- **Entry Point:** `src/temporal.ps1`
- **Dependencies:** mempalacenet >=0.7.0

### 4. Hybrid Search + Reranking
- **ID:** `hybrid-search-reranking`
- **Version:** 1.5.0
- **Tags:** search, hybrid, reranking, llm
- **Description:** LLM-based reranking for hybrid semantic/keyword search
- **Entry Point:** `src/rerank.ps1`
- **Dependencies:** mempalacenet >=0.7.0, Microsoft.Extensions.AI >=10.3.0

---

## Skill Folder Structure

Skills are organized in `~/.squad/skills/` with the following structure:

```
~/.squad/skills/
├── rag-context-injector/
│   ├── skill.json           # Manifest (metadata)
│   ├── SKILL.md             # Documentation
│   ├── README.md            # Usage guide
│   ├── LICENSE              # License file
│   └── src/
│       ├── run.ps1          # Entry point
│       ├── RagInjector.cs   # Implementation
│       └── ...
├── agent-diary/
│   ├── skill.json
│   ├── SKILL.md
│   ├── src/diary.ps1
│   └── ...
└── ...
```

### Manifest Schema (`skill.json`)

```json
{
  "id": "rag-context-injector",
  "name": "RAG Context Injector",
  "version": "1.0.0",
  "description": "Semantic search + LLM context injection",
  "author": "Bruno Capuano",
  "entryPoint": "src/run.ps1",
  "tags": ["rag", "semantic-search", "llm"],
  "dependencies": {
    "mempalacenet": ">=0.7.0",
    "Microsoft.Extensions.AI": ">=10.3.0"
  },
  "enabled": true,
  "discoverable": true,
  "repository": "https://github.com/elbruno/mempalacenet-skills",
  "license": "MIT"
}
```

---

## Roadmap

### v0.7.0 ✅ Complete
- ✅ Local skill discovery (`skill discover`)
- ✅ Enhanced `skill list` with filters
- ✅ Built-in demo skills (4 skills)
- ✅ Local installation from paths
- ✅ Enable/disable/uninstall operations
- ✅ Rich Spectre.Console UX

### v1.0 (Planned)
- 🚧 Remote registry API (skills.mempalacenet.dev)
- 🚧 Remote skill installation (`skill install <id> --from remote`)
- 🚧 Version management & constraints
- 🚧 Dependency resolution
- 🚧 Skill marketplace web portal
- 🚧 Community skill submissions

### Future (v2.0+)
- 🔮 Skill marketplace with ratings & reviews
- 🔮 Automated updates & version checks
- 🔮 Skill bundling & templates
- 🔮 Interactive skill generator

---

## FAQ

**Q: Where are skills installed?**  
A: Skills are installed to `~/.squad/skills/` by default.

**Q: How do I create my own skill?**  
A: See [SKILL_MANIFEST.md](./skill-manifest-schema.md) for the complete schema and examples.

**Q: Can I use skills from v1.0 in v0.7.0?**  
A: Not automatically, but local skills with compatible dependencies will work. Remote registry coming in v1.0.

**Q: How do I share a skill?**  
A: For v0.7.0, share via GitHub or as a ZIP archive. Users can clone/download and install locally. Automated sharing coming in v1.0.

**Q: What if a skill depends on a feature I don't have?**  
A: The skill will still install, but may fail at runtime. Check dependencies before installing.

---

## See Also

- [SKILL_MANIFEST.md](./skill-manifest-schema.md) — Full manifest schema documentation
- [Phase 3 Decisions](../.squad/decisions/inbox/rachael-skill-marketplace-phase3.md) — Architecture decisions
- [README.md](../../README.md) — Main project README
