# Skill Marketplace CLI Design Specification

**Version:** v0.7.0 MVP  
**Last Updated:** 2025-04-25  
**Owner:** Rachael (CLI/UX Dev)

---

## Overview

The **Skill Marketplace** enables distribution and discovery of reusable MemPalace.NET patterns, code snippets, and integrations. This document specifies the CLI surface, folder structure, and user workflows for the v0.7.0 MVP.

**Key Goals:**
1. ✅ Enable **local filesystem-based skill sharing** (no remote registry required)
2. ✅ Provide **rich CLI UX** using Spectre.Console (tables, panels, progress bars)
3. ✅ Establish **folder structure** for future remote distribution (v1.0)
4. ⚠️ **Defer:** Remote registry, dependency resolution, skill versioning (v1.0+)

**Architecture Overview:**

```
┌─────────────────────────────────────────────────────────────┐
│                    mempalace CLI                            │
│  (Spectre.Console.Cli + Microsoft.Extensions.DI)           │
├─────────────────────────────────────────────────────────────┤
│  skill list │ search │ info │ enable │ disable │ uninstall │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
           ┌────────────────────────────┐
           │  Local Skill Registry      │
           │  (~/.palace/skills/)       │
           │  - skill.json (manifest)   │
           │  - README.md               │
           │  - src/*.cs                │
           └────────────────────────────┘
                        │
                        │ (v1.0: MCP SSE Transport)
                        ▼
           ┌────────────────────────────┐
           │  Remote Skill Registry     │
           │  (skills.mempalacenet.dev) │
           └────────────────────────────┘
```

---

## CLI Command Reference

### Command Tree

```
mempalace skill
├── list [--available|--installed|--enabled]
├── search <query> [--tag <tag>]
├── info <skill-id>
├── install <skill-id> [--version <version>]
├── enable <skill-id>
├── disable <skill-id>
└── uninstall <skill-id>
```

---

## Command: `mempalace skill list`

### Description
List available, installed, or enabled skills in the local registry.

### Syntax
```bash
mempalace skill list [options]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--available` | Show all discoverable skills (local + remote) | false |
| `--installed` | Show only installed skills | false |
| `--enabled` | Show only enabled skills | false |
| (none) | Show all installed skills | true |

### Examples

**1. List all installed skills:**
```bash
$ mempalace skill list
```

**Output:**
```
┌────────────────────────────┬─────────┬──────────────┬─────────────────────────────────────┐
│ Skill ID                   │ Version │ Status       │ Description                         │
├────────────────────────────┼─────────┼──────────────┼─────────────────────────────────────┤
│ rag-context-injector       │ 1.0.0   │ ✅ Enabled   │ Semantic search + LLM context       │
│ agent-diary-persistence    │ 2.1.0   │ ✅ Enabled   │ Agent state persistence across...   │
│ hybrid-search-reranking    │ 1.5.0   │ ⚠️ Disabled  │ LLM-based reranking for hybrid...   │
│ kg-temporal-queries        │ 0.8.0   │ ✅ Enabled   │ Knowledge graph with temporal...    │
└────────────────────────────┴─────────┴──────────────┴─────────────────────────────────────┘

4 skills installed (3 enabled, 1 disabled)
```

**2. List only enabled skills:**
```bash
$ mempalace skill list --enabled
```

**Output:**
```
┌────────────────────────────┬─────────┬─────────────────────────────────────┐
│ Skill ID                   │ Version │ Description                         │
├────────────────────────────┼─────────┼─────────────────────────────────────┤
│ rag-context-injector       │ 1.0.0   │ Semantic search + LLM context       │
│ agent-diary-persistence    │ 2.1.0   │ Agent state persistence across...   │
│ kg-temporal-queries        │ 0.8.0   │ Knowledge graph with temporal...    │
└────────────────────────────┴─────────┴─────────────────────────────────────┘

3 enabled skills
```

### Exit Codes
- `0` - Success
- `1` - Error (e.g., skill directory not found)

---

## Command: `mempalace skill search`

### Description
Search for skills by name, description, or tags.

### Syntax
```bash
mempalace skill search <query> [options]
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<query>` | Search query (matches name, description, tags) | ✅ |

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--tag <tag>` | Filter by tag (case-insensitive) | (none) |
| `--remote` | Search remote registry (v1.0 only) | false |

### Examples

**1. Search by keyword:**
```bash
$ mempalace skill search "rag"
```

**Output:**
```
Found 2 skills matching "rag":

┌────────────────────────────┬─────────┬──────────────────────────────────────┐
│ Skill ID                   │ Version │ Description                          │
├────────────────────────────┼─────────┼──────────────────────────────────────┤
│ rag-context-injector       │ 1.0.0   │ Semantic search + LLM context        │
│ hybrid-rag-fusion          │ 0.9.0   │ Multi-vector RAG with reranking      │
└────────────────────────────┴─────────┴──────────────────────────────────────┘

Tags: rag, semantic-search, llm, context, hybrid
```

**2. Search by tag:**
```bash
$ mempalace skill search "context" --tag semantic-search
```

**Output:**
```
Found 1 skill with tag "semantic-search":

┌────────────────────────────┬─────────┬──────────────────────────────────────┐
│ Skill ID                   │ Version │ Description                          │
├────────────────────────────┼─────────┼──────────────────────────────────────┤
│ rag-context-injector       │ 1.0.0   │ Semantic search + LLM context        │
└────────────────────────────┴─────────┴──────────────────────────────────────┘
```

### Exit Codes
- `0` - Success (results found)
- `1` - No results found or error

---

## Command: `mempalace skill info`

### Description
Display detailed information about a skill (manifest, README, dependencies).

### Syntax
```bash
mempalace skill info <skill-id>
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<skill-id>` | Unique skill identifier (kebab-case) | ✅ |

### Examples

**1. Display skill info:**
```bash
$ mempalace skill info rag-context-injector
```

**Output:**
```
╔═══════════════════════════════════════════════════════════════════════╗
║                       RAG Context Injector                            ║
╠═══════════════════════════════════════════════════════════════════════╣
║ ID:          rag-context-injector                                     ║
║ Version:     1.0.0                                                    ║
║ Author:      Bruno Capuano                                            ║
║ License:     MIT                                                      ║
║ Status:      ✅ Installed, Enabled                                    ║
║ Repository:  https://github.com/elbruno/mempalacenet-skills          ║
╠═══════════════════════════════════════════════════════════════════════╣
║ Description:                                                          ║
║ Semantic search + LLM context injection for RAG workflows            ║
║                                                                       ║
║ Dependencies:                                                         ║
║   • mempalacenet >= 0.7.0                                             ║
║   • Microsoft.Extensions.AI >= 10.3.0                                 ║
║                                                                       ║
║ Tags: rag, semantic-search, llm, context                              ║
╚═══════════════════════════════════════════════════════════════════════╝

README:
─────────────────────────────────────────────────────────────────────────
Use MemPalace.NET as a RAG source to inject relevant context into LLM
prompts. This skill demonstrates how to:

1. Store documents/conversations in a palace
2. Perform semantic search to retrieve relevant memories
3. Inject retrieved context into an LLM prompt
4. Generate a contextually-aware response

Usage Example:
──────────────
var palace = await Palace.Create("~/my-palace");
var chatClient = new OpenAIChatClient("gpt-4o", apiKey);
var injector = new RagContextInjector(palace, chatClient);

var answer = await injector.AnswerWithContext(
    question: "How do I implement OAuth2?",
    wing: "documentation"
);
```

### Exit Codes
- `0` - Success
- `1` - Skill not found or invalid manifest

---

## Command: `mempalace skill install`

### Description
Install a skill from the local filesystem or remote registry (v1.0).

### Syntax
```bash
mempalace skill install <skill-id> [options]
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<skill-id>` | Unique skill identifier | ✅ |

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--version <version>` | Specific version to install (semver) | latest |
| `--no-enable` | Don't enable skill after install | false |
| `--from <source>` | Install source: `local` or `remote` (v1.0) | local |

### Examples

**1. Install skill (v0.7.0 MVP — manual instructions):**
```bash
$ mempalace skill install rag-context-injector
```

**Output:**
```
⚠️  v0.7.0 MVP: Manual installation required.

Instructions:
─────────────────────────────────────────────────────────────────────────
1. Download skill from:
   https://github.com/elbruno/mempalacenet-skills/tree/main/rag-context-injector

2. Copy folder to:
   C:\Users\brunocapuano\.palace\skills\rag-context-injector\

   Your folder structure should look like:
   C:\Users\brunocapuano\.palace\skills\
   └── rag-context-injector\
       ├── skill.json
       ├── README.md
       └── src\
           └── RagContextInjector.cs

3. Enable the skill:
   mempalace skill enable rag-context-injector

(Automatic installation coming in v1.0 with remote registry)
```

**2. Install skill (v1.0 — automatic download):**
```bash
$ mempalace skill install rag-context-injector --from remote
```

**Output:**
```
📦 Downloading skill 'rag-context-injector' (v1.0.0)...
[████████████████████████████████████████] 100%

✅ Skill 'rag-context-injector' installed successfully

Dependencies installed:
  • mempalacenet 0.7.0 (already satisfied)
  • Microsoft.Extensions.AI 10.3.0 (already satisfied)

Run 'mempalace skill enable rag-context-injector' to activate.
```

### Exit Codes
- `0` - Success
- `1` - Skill not found, invalid manifest, or installation error

---

## Command: `mempalace skill enable`

### Description
Enable an installed skill for use in palace operations.

### Syntax
```bash
mempalace skill enable <skill-id>
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<skill-id>` | Unique skill identifier | ✅ |

### Examples

**1. Enable a skill:**
```bash
$ mempalace skill enable rag-context-injector
```

**Output:**
```
✅ Skill 'rag-context-injector' enabled

The skill will be available for use in your palace operations.

Next steps:
  • Run 'mempalace skill info rag-context-injector' for usage examples
  • View documentation: C:\Users\brunocapuano\.palace\skills\rag-context-injector\README.md
```

### Exit Codes
- `0` - Success
- `1` - Skill not found or already enabled

---

## Command: `mempalace skill disable`

### Description
Disable an enabled skill (doesn't uninstall).

### Syntax
```bash
mempalace skill disable <skill-id>
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<skill-id>` | Unique skill identifier | ✅ |

### Examples

**1. Disable a skill:**
```bash
$ mempalace skill disable hybrid-search-reranking
```

**Output:**
```
⚠️  Skill 'hybrid-search-reranking' disabled

The skill will no longer be used in palace operations.
You can re-enable it with: mempalace skill enable hybrid-search-reranking
```

### Exit Codes
- `0` - Success
- `1` - Skill not found or already disabled

---

## Command: `mempalace skill uninstall`

### Description
Remove a skill from the local registry (deletes folder).

### Syntax
```bash
mempalace skill uninstall <skill-id> [options]
```

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `<skill-id>` | Unique skill identifier | ✅ |

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--force` | Skip confirmation prompt | false |

### Examples

**1. Uninstall with confirmation:**
```bash
$ mempalace skill uninstall rag-context-injector
```

**Output:**
```
⚠️  This will permanently delete the skill folder:
   C:\Users\brunocapuano\.palace\skills\rag-context-injector\

Proceed? [y/N]: y

✅ Skill 'rag-context-injector' uninstalled
```

**2. Uninstall without confirmation:**
```bash
$ mempalace skill uninstall rag-context-injector --force
```

**Output:**
```
✅ Skill 'rag-context-injector' uninstalled
```

### Exit Codes
- `0` - Success
- `1` - Skill not found, user cancelled, or deletion error

---

## Folder Structure

### Skill Root Directory

**Location:** `~/.palace/skills/` (platform-specific user directory)

- **Windows:** `C:\Users\{username}\.palace\skills\`
- **macOS/Linux:** `/home/{username}/.palace/skills/`

### Per-Skill Layout

```
~/.palace/skills/
├── rag-context-injector/           # Skill ID (kebab-case)
│   ├── skill.json                  # Manifest (required)
│   ├── README.md                   # Documentation (required)
│   ├── LICENSE                     # License file (optional)
│   ├── src/                        # Implementation files
│   │   ├── RagContextInjector.cs
│   │   └── Extensions.cs
│   ├── examples/                   # Usage examples (optional)
│   │   └── usage.md
│   └── tests/                      # Unit tests (optional)
│       └── RagTests.cs
│
├── agent-diary-persistence/
│   ├── skill.json
│   ├── README.md
│   ├── src/
│   │   ├── AgentDiary.cs
│   │   └── DiaryStorage.cs
│   └── examples/
│       └── chatbot-memory.md
│
└── ...
```

### Required Files

| File | Required | Description |
|------|----------|-------------|
| `skill.json` | ✅ | Manifest with metadata and dependencies |
| `README.md` | ✅ | User documentation with examples |
| `LICENSE` | ❌ | License file (recommended: MIT, Apache-2.0) |
| `src/` | ❌ | Implementation files (C#, scripts, etc.) |
| `examples/` | ❌ | Usage examples and tutorials |
| `tests/` | ❌ | Unit tests (xUnit, NUnit, etc.) |

---

## Skill Manifest Schema

### `skill.json` Format

```json
{
  "$schema": "https://mempalacenet.dev/schemas/skill-v1.json",
  "id": "rag-context-injector",
  "name": "RAG Context Injector",
  "version": "1.0.0",
  "description": "Semantic search + LLM context injection for RAG workflows",
  "author": "Bruno Capuano",
  "license": "MIT",
  "tags": ["rag", "semantic-search", "llm", "context"],
  "dependencies": {
    "mempalacenet": ">=0.7.0",
    "Microsoft.Extensions.AI": ">=10.3.0"
  },
  "entry_point": "src/RagContextInjector.cs",
  "repository": "https://github.com/elbruno/mempalacenet-skills",
  "homepage": "https://github.com/elbruno/mempalacenet-skills/tree/main/rag-context-injector",
  "created_at": "2025-04-25T00:00:00Z",
  "updated_at": "2025-04-25T00:00:00Z"
}
```

### Field Reference

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `$schema` | string (URL) | ❌ | JSON schema reference | Must be valid URL |
| `id` | string | ✅ | Unique skill identifier | Kebab-case, 3-50 chars |
| `name` | string | ✅ | Human-readable name | 3-100 chars |
| `version` | string | ✅ | Semantic version | Valid semver (e.g., 1.0.0) |
| `description` | string | ✅ | One-line summary | 10-200 chars |
| `author` | string | ✅ | Author name or GitHub handle | 2-100 chars |
| `license` | string | ❌ | SPDX license identifier | SPDX format (MIT, Apache-2.0) |
| `tags` | array[string] | ❌ | Searchable tags | Lowercase, 1-20 chars each |
| `dependencies` | object | ❌ | Package dependencies | Semver ranges |
| `entry_point` | string | ❌ | Main file path | Relative to skill root |
| `repository` | string (URL) | ❌ | Source repository | Valid Git URL |
| `homepage` | string (URL) | ❌ | Documentation URL | Valid URL |
| `created_at` | string | ❌ | Creation timestamp | ISO8601 UTC |
| `updated_at` | string | ❌ | Last update timestamp | ISO8601 UTC |

### Example: Minimal Manifest

```json
{
  "id": "simple-search",
  "name": "Simple Semantic Search",
  "version": "0.1.0",
  "description": "Basic semantic search example",
  "author": "Jane Doe"
}
```

### Example: Full Manifest with Dependencies

```json
{
  "$schema": "https://mempalacenet.dev/schemas/skill-v1.json",
  "id": "advanced-kg-queries",
  "name": "Advanced Knowledge Graph Queries",
  "version": "2.5.3",
  "description": "Temporal queries with relationship traversal and graph algorithms",
  "author": "John Smith (@jsmith)",
  "license": "MIT",
  "tags": ["knowledge-graph", "temporal", "graph-algorithms", "cypher"],
  "dependencies": {
    "mempalacenet": ">=0.7.0 <2.0.0",
    "Microsoft.Extensions.AI": "^10.3.0",
    "QuikGraph": ">=2.5.0"
  },
  "entry_point": "src/AdvancedKgQueries.cs",
  "repository": "https://github.com/jsmith/mempalacenet-skills",
  "homepage": "https://jsmith.dev/kg-skills",
  "created_at": "2025-01-15T10:00:00Z",
  "updated_at": "2025-04-20T14:30:00Z"
}
```

---

## User Workflows

### Workflow 1: Discover and Install a Skill (v0.7.0)

```bash
# 1. Search for skills related to "rag"
$ mempalace skill search "rag"
Found 2 skills...

# 2. View details about a specific skill
$ mempalace skill info rag-context-injector
[Displays manifest + README]

# 3. Manual install (v0.7.0)
$ mempalace skill install rag-context-injector
⚠️  Manual installation required...
[Follow instructions to download and copy]

# 4. Enable the skill
$ mempalace skill enable rag-context-injector
✅ Skill enabled

# 5. Verify installation
$ mempalace skill list --enabled
rag-context-injector  1.0.0  ✅ Enabled
```

### Workflow 2: Manage Installed Skills

```bash
# List all installed skills
$ mempalace skill list

# Disable a skill temporarily
$ mempalace skill disable hybrid-search-reranking
⚠️  Skill disabled

# Re-enable later
$ mempalace skill enable hybrid-search-reranking
✅ Skill enabled

# Uninstall a skill
$ mempalace skill uninstall old-skill --force
✅ Skill uninstalled
```

### Workflow 3: Create and Share a Custom Skill

**Step 1: Create skill folder**
```bash
$ mkdir -p ~/.palace/skills/my-custom-skill
$ cd ~/.palace/skills/my-custom-skill
```

**Step 2: Create `skill.json` manifest**
```json
{
  "id": "my-custom-skill",
  "name": "My Custom Skill",
  "version": "1.0.0",
  "description": "Custom skill for my use case",
  "author": "Your Name"
}
```

**Step 3: Create `README.md` documentation**
```markdown
# My Custom Skill

## Overview
This skill does XYZ...

## Usage
```csharp
var palace = await Palace.Create("~/my-palace");
// Your code here
```
```

**Step 4: Add implementation files**
```bash
$ mkdir src
$ touch src/MyCustomSkill.cs
```

**Step 5: Enable and test**
```bash
$ mempalace skill enable my-custom-skill
✅ Skill enabled

$ mempalace skill list --enabled
my-custom-skill  1.0.0  ✅ Enabled
```

**Step 6: Share on GitHub**
```bash
$ cd ~/.palace/skills/my-custom-skill
$ git init
$ git add .
$ git commit -m "Initial commit"
$ git remote add origin https://github.com/yourusername/mempalacenet-skills
$ git push -u origin main
```

---

## Configuration

### `~/.palace/config.json` Schema

```json
{
  "palace": {
    "path": "~/.palace",
    "backend": "sqlite"
  },
  "skills": {
    "enabled": [
      "rag-context-injector",
      "agent-diary-persistence",
      "kg-temporal-queries"
    ],
    "disabled": [
      "hybrid-search-reranking"
    ]
  },
  "skill_registry": {
    "url": "wss://skills.mempalacenet.dev/sse",
    "enabled": false,
    "cache_ttl_hours": 24
  }
}
```

### Configuration Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `skills.enabled` | array[string] | List of enabled skill IDs | `[]` |
| `skills.disabled` | array[string] | List of disabled skill IDs | `[]` |
| `skill_registry.url` | string (URL) | Remote registry endpoint (v1.0) | (none) |
| `skill_registry.enabled` | boolean | Enable remote registry discovery | false |
| `skill_registry.cache_ttl_hours` | integer | Cache TTL for remote metadata | 24 |

---

## Error Handling

### Error Messages

| Error | Message | Exit Code |
|-------|---------|-----------|
| Skill not found | `❌ Error: Skill 'xyz' not found in ~/.palace/skills/` | 1 |
| Invalid manifest | `❌ Error: Invalid skill.json in 'xyz': missing required field 'version'` | 1 |
| Already enabled | `⚠️  Skill 'xyz' is already enabled` | 0 |
| Already disabled | `⚠️  Skill 'xyz' is already disabled` | 0 |
| Permission denied | `❌ Error: Permission denied when writing to ~/.palace/config.json` | 1 |
| Network error (v1.0) | `❌ Error: Failed to connect to remote registry (timeout)` | 1 |

### Validation Errors

**Invalid Manifest:**
```bash
$ mempalace skill info broken-skill
❌ Error: Invalid skill.json in 'broken-skill'

Missing required fields:
  • version (required semver string)
  • description (required string, 10-200 chars)

Please fix the manifest and try again.
```

**Invalid Skill ID:**
```bash
$ mempalace skill enable Invalid_Skill_Name
❌ Error: Invalid skill ID format

Skill IDs must be:
  • Lowercase kebab-case (e.g., "my-skill")
  • 3-50 characters
  • Letters, numbers, and hyphens only

Example: mempalace skill enable my-custom-skill
```

---

## Future Enhancements (v1.0+)

### 1. Remote Registry Integration
- MCP SSE transport for skill discovery
- Automatic download and installation
- Skill ratings and popularity metrics
- User reviews and comments

### 2. Dependency Resolution
- Parse `dependencies` field in manifest
- Check for version conflicts
- Auto-install missing dependencies
- Dependency graph visualization

### 3. Skill Versioning
- Support multiple versions: `~/.palace/skills/{skill-id}@{version}/`
- Update notifications
- Rollback to previous versions
- Semver range resolution

### 4. Skill Security
- Code signing with GPG/PGP
- Trusted publisher verification
- Sandboxed execution (AppDomain isolation)
- Permission system for file/network access

### 5. Advanced CLI Features
- `mempalace skill update [skill-id]` - Update to latest version
- `mempalace skill publish` - Publish skill to remote registry
- `mempalace skill validate` - Lint manifest and check best practices
- `mempalace skill deps` - Visualize dependency tree

---

## Testing Strategy

### Unit Tests

**Test Coverage:**
- Manifest parsing and validation
- Skill discovery (filesystem scan)
- Search logic (name, description, tags)
- Enable/disable state management
- Uninstall (folder deletion)

**Example Test:**
```csharp
[Fact]
public async Task SearchSkills_MatchesName()
{
    var skills = await _skillRegistry.SearchAsync("rag");
    
    Assert.Contains(skills, s => s.Id == "rag-context-injector");
    Assert.DoesNotContain(skills, s => s.Id == "kg-temporal-queries");
}
```

### Integration Tests

**Test Scenarios:**
1. End-to-end skill installation workflow
2. Enable/disable/uninstall lifecycle
3. Config file updates (idempotency)
4. Error handling (missing files, invalid manifests)

### Manual Testing

**Test Checklist:**
- [ ] `mempalace skill list` displays correct table
- [ ] `mempalace skill search` filters by query and tag
- [ ] `mempalace skill info` renders manifest + README
- [ ] `mempalace skill enable` updates config.json
- [ ] `mempalace skill disable` updates config.json
- [ ] `mempalace skill uninstall` prompts for confirmation
- [ ] All commands handle missing skills gracefully
- [ ] Help text (`--help`) is accurate and complete

---

## References

- **ADR:** `.squad/decisions/inbox/rachael-skill-marketplace-cli.md`
- **Copilot Skill Patterns:** `docs/SKILL_PATTERNS.md`
- **CLI Reference:** `docs/cli.md`
- **MCP SSE Transport:** `.squad/decisions/inbox/tyrell-v070-mcp-sse-transport.md` (Tyrell's design)
- **v0.7.0 Roadmap:** `.squad/decisions/inbox/deckard-v070-roadmap-proposal.md`

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-04-25 | Initial design spec for v0.7.0 MVP |

---

**End of Specification**
