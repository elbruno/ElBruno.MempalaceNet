# Skill Manifest Schema

**Version:** v0.7.0 MVP  
**File Name:** `skill.json` (or `manifest.json`)  
**Location:** `~/.squad/skills/{skill-id}/skill.json`

---

## Overview

The **Skill Manifest** is a JSON file that describes a MemPalace.NET skill, including its metadata, dependencies, and entry point. Every skill must include a valid manifest for installation and discovery.

---

## Schema

```json
{
  "id": "rag-context-injector",
  "name": "RAG Context Injector",
  "version": "1.0.0",
  "description": "Semantic search + LLM context injection for RAG workflows",
  "author": "Bruno Capuano",
  "entryPoint": "src/run.ps1",
  "tags": ["rag", "semantic-search", "llm", "context"],
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

## Field Reference

### Required Fields

#### `id` (string)
- **Description:** Unique skill identifier (immutable)
- **Format:** kebab-case (lowercase, hyphens only, no spaces)
- **Example:** `rag-context-injector`
- **Rules:** 
  - Must be 3-32 characters
  - Must start with a letter
  - Cannot contain underscores, uppercase, or special characters (except hyphens)
  - Must be unique across all skills

#### `name` (string)
- **Description:** Human-readable skill name
- **Example:** `RAG Context Injector`
- **Rules:**
  - Max 64 characters
  - Can contain spaces, uppercase, punctuation
  - Should be descriptive but concise

#### `version` (string)
- **Description:** Semantic version (semver 2.0.0)
- **Example:** `1.0.0`, `2.1.0`, `0.8.0-beta.1`
- **Rules:**
  - Format: `MAJOR.MINOR.PATCH[-prerelease][+build]`
  - Example valid: `1.0.0`, `2.1.3`, `1.0.0-rc.1`, `1.0.0+build.123`
  - Must be comparable (for future version constraints)

#### `description` (string)
- **Description:** Short skill description
- **Example:** `Semantic search + LLM context injection for RAG workflows`
- **Rules:**
  - Max 200 characters
  - Should start with a capital letter
  - Should describe the main purpose/value

#### `entryPoint` (string)
- **Description:** Relative path to the skill's entry point script
- **Example:** `src/run.ps1`, `src/main.py`, `lib/invoke.sh`
- **Rules:**
  - Path relative to skill root directory
  - Must be an executable file
  - Can be PowerShell (.ps1), Python (.py), Bash (.sh), or other executable
  - File must exist in the skill package

---

### Optional Fields

#### `author` (string)
- **Description:** Author or organization name
- **Example:** `Bruno Capuano`, `ElBruno`, `MemPalace Contributors`
- **Default:** (omitted if unknown)
- **Rules:**
  - Max 100 characters
  - Can include spaces and punctuation

#### `tags` (array of strings)
- **Description:** Skill categories/labels for discovery
- **Example:** `["rag", "semantic-search", "llm", "context"]`
- **Default:** `[]` (empty array)
- **Rules:**
  - Each tag: kebab-case, 3-20 characters
  - Max 10 tags per skill
  - Used for filtering in `skill discover --tag <tag>`
  - Common tags: rag, agents, persistence, knowledge-graph, search, hybrid, reranking, llm, temporal, etc.

#### `dependencies` (object)
- **Description:** External package or skill dependencies
- **Example:**
  ```json
  {
    "mempalacenet": ">=0.7.0",
    "Microsoft.Extensions.AI": ">=10.3.0"
  }
  ```
- **Default:** `{}` (empty object)
- **Rules:**
  - Keys: package names or skill IDs
  - Values: version constraints (e.g., `>=1.0.0`, `^2.0`, `~1.5`, `1.0.0`)
  - Format: similar to npm/package.json
  - v0.7.0: Dependencies are documented but NOT validated on install
  - v1.0+: Full dependency resolution planned

#### `enabled` (boolean)
- **Description:** Whether the skill is active/enabled
- **Default:** `true`
- **Rules:**
  - Modified by `mempalacenet skill enable/disable`
  - Disabled skills remain installed but are not activated
  - Allows for "soft uninstall" without data loss

#### `discoverable` (boolean)
- **Description:** Whether the skill appears in `skill discover` output
- **Default:** `true`
- **Rules:**
  - If `false`, skill exists but won't appear in discovery (hidden skill)
  - Can be manually toggled for advanced scenarios
  - Useful for beta/experimental skills

#### `repository` (string)
- **Description:** Public repository URL (GitHub, GitLab, etc.)
- **Example:** `https://github.com/elbruno/mempalacenet-skills`
- **Default:** (omitted if not applicable)
- **Rules:**
  - Should be a valid HTTP(S) URL
  - Used for linking to source code and documentation

#### `license` (string)
- **Description:** SPDX license identifier
- **Example:** `MIT`, `Apache-2.0`, `GPL-3.0-only`, `BSD-2-Clause`
- **Default:** (omitted if unknown)
- **Reference:** [SPDX License List](https://spdx.org/licenses/)
- **Rules:**
  - Should use official SPDX identifier
  - Helps users understand usage rights

---

## Examples

### Example 1: Minimal Skill

```json
{
  "id": "hello-world",
  "name": "Hello World",
  "version": "0.1.0",
  "description": "A simple greeting skill",
  "entryPoint": "run.ps1"
}
```

### Example 2: RAG Skill (Complex)

```json
{
  "id": "rag-context-injector",
  "name": "RAG Context Injector",
  "version": "1.0.0",
  "description": "Semantic search + LLM context injection for RAG workflows",
  "author": "Bruno Capuano",
  "entryPoint": "src/run.ps1",
  "tags": ["rag", "semantic-search", "llm", "context", "retrieval"],
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

### Example 3: Python-based Skill

```json
{
  "id": "python-embedder",
  "name": "Python Embedder",
  "version": "2.0.0",
  "description": "Generate embeddings using Python transformers",
  "author": "Data Science Team",
  "entryPoint": "src/embed.py",
  "tags": ["embeddings", "ml", "python"],
  "dependencies": {
    "python": ">=3.10",
    "transformers": ">=4.30.0",
    "torch": ">=2.0.0"
  },
  "enabled": true,
  "discoverable": true,
  "repository": "https://github.com/myorg/python-embedder",
  "license": "Apache-2.0"
}
```

### Example 4: Disabled/Hidden Skill

```json
{
  "id": "experimental-feature",
  "name": "Experimental Feature",
  "version": "0.1.0-beta",
  "description": "Beta feature under development",
  "entryPoint": "src/beta.ps1",
  "enabled": false,
  "discoverable": false
}
```

---

## Validation Rules

### On Installation
1. ✅ All required fields must be present
2. ✅ Field types must be correct (strings, objects, booleans)
3. ✅ `id` must match skill directory name
4. ✅ `entryPoint` file must exist in the skill package
5. ⚠️  Dependencies are validated but not enforced (v0.7.0)

### On Discovery
1. ✅ `discoverable: true` is required to appear in results
2. ✅ `enabled` status is displayed to users

---

## Best Practices

### Naming
- Use clear, descriptive skill IDs and names
- Avoid generic names like "helper" or "util"
- Use author/org prefix for clarity (e.g., `bruno-rag-skill`)

### Versioning
- Start at `1.0.0` or `0.1.0` for experimental
- Use semantic versioning strictly
- Include pre-release tags for beta/rc versions

### Tags
- Use 3-5 most relevant tags
- Prefer lowercase, hyphenated tags
- Consider: technology (rag, llm, agents), domain (knowledge-graph, search), capability (reranking, persistence)

### Documentation
- Include `README.md` with usage examples
- Document dependencies clearly
- List any environment variables or config needed

### Dependencies
- Be specific with version constraints (e.g., `>=1.0.0`, not `*`)
- List only direct dependencies, not transitive ones
- Test skill with minimum required versions

---

## Future Enhancements (v1.0+)

- 🔮 Dependency resolution and validation
- 🔮 Version constraint enforcement
- 🔮 Automated version checking
- 🔮 Schema validation on install
- 🔮 Digital signatures for trusted skills
- 🔮 Skill metadata versioning

---

## See Also

- [Skill Discovery Guide](./skill-discovery.md)
- [Phase 3 Architecture](../.squad/decisions/inbox/rachael-skill-marketplace-phase3.md)
- [SkillManifest.cs](../../src/MemPalace.Core/Model/SkillManifest.cs) — C# model
