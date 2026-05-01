# Getting Started with MemPalace.NET

**Welcome!** This guide walks you through creating and using your first memory palace end-to-end. Whether you're exploring the system or preparing to integrate it into your app, you'll learn by doing.

**Read time:** ~8 minutes | **Difficulty:** Beginner-friendly

---

## Prerequisites

Before you start, ensure you have:

- **.NET 10.0+** installed (`dotnet --version` to check)
- **git** (optional, for cloning the repo)
- **Write access** to your home directory or a local folder where you'll store your palace
- **A text editor or IDE** (VS Code, Visual Studio, or any editor you prefer)

If you don't have .NET installed, download it from [dotnet.microsoft.com](https://dotnet.microsoft.com).

---

## Installation

### Option A: Install from NuGet (Recommended for Most Users)

```bash
dotnet tool install -g mempalacenet --version 0.6.0
```

Verify the installation:

```bash
mempalacenet --version
```

Expected output:

```
mempalacenet 0.6.0
```

### Option B: Build from Source

If you want to build from the latest main branch:

```bash
# Clone the repository
git clone https://github.com/elbruno/mempalacenet.git
cd mempalacenet

# Build the solution
dotnet build

# Pack and install locally
dotnet pack src/MemPalace.Cli/MemPalace.Cli.csproj -c Release -o ./publish
dotnet tool install -g mempalacenet --add-source ./publish --version 0.6.0
```

---

## Step-by-Step Workflow

### Step 1: Initialize a Palace

A **palace** is your personal knowledge repository. Let's create one.

#### Command

```bash
mempalacenet init ~/my-palace
```

#### What Happens

```
✓ Palace initialized at /Users/yourname/my-palace
  - Created directory structure
  - Initialized SQLite database (palace.db)
  - Created .mempalace/ metadata folder
  - Ready to store memories!
```

#### Explore the Structure

```bash
ls -la ~/my-palace
```

You'll see:

```
palace.db              # SQLite database (stores memories & embeddings)
.mempalace/            # Metadata folder
  ├─ agents/           # Agent descriptors (YAML)
  ├─ config.json       # Palace configuration
  └─ metadata.json     # Timestamps, palace info
```

**Understanding the Structure:**

- **palace.db**: Main data store with embeddings, relationships, and metadata
- **.mempalace/**: Hidden config directory (one per palace)
- **wings**: Top-level organizational categories (you'll add these as you store memories)

---

### Step 2: Mine Your First Memories

**Mining** means extracting content from files or conversations and storing them in the palace with semantic embeddings.

#### A. Mine Files (Documentation/Code)

Let's mine some documentation:

```bash
mempalacenet mine ~/my-palace ~/my-docs \
  --wing documentation \
  --mode files
```

If you don't have docs yet, you can create sample files:

```bash
mkdir -p ~/my-docs
cat > ~/my-docs/auth.md << 'EOF'
# Authentication Best Practices

## Overview
Authentication verifies user identity through credentials like passwords, tokens, or biometrics.

## JWT (JSON Web Tokens)
- Stateless tokens with encoded claims
- Signed with a secret key
- Good for microservices and distributed systems
- Include an expiration time (exp claim)

## OAuth 2.0
- Industry standard for delegated access
- Used by Google, GitHub, and social providers
- Separates authentication from authorization

## Best Practices
1. Always use HTTPS for auth endpoints
2. Hash passwords with bcrypt or Argon2
3. Implement rate limiting on login attempts
4. Use secure session cookies (httpOnly, Secure flags)
EOF

# Mine it
mempalacenet mine ~/my-palace ~/my-docs \
  --wing documentation \
  --mode files
```

#### Expected Output

```
Mining files from: /Users/yourname/my-docs
Target wing: documentation

Processing:
  ✓ auth.md (412 bytes)
  ✓ Extracted 3 chunks
  ✓ Generated embeddings

Summary:
  Files processed: 1
  Total chunks: 3
  Embeddings: 3
  Status: Success
```

**What Just Happened:**

1. Files were read from `~/my-docs`
2. Content was chunked into semantic pieces
3. Each chunk was embedded using local ONNX embeddings (no API calls!)
4. Memories were stored in the "documentation" wing with metadata
5. Everything is searchable by semantic meaning

#### B. Mine Conversations (Optional)

If you have conversation transcripts:

```bash
cat > ~/my-convos.jsonl << 'EOF'
{"role": "user", "content": "How do I implement caching?"}
{"role": "assistant", "content": "Caching stores frequently accessed data in memory to reduce latency. Strategies include LRU (least recently used), TTL-based expiry, and write-through patterns."}
{"role": "user", "content": "What about distributed caching?"}
{"role": "assistant", "content": "Distributed caching uses Redis or Memcached to share cache across services. Ensures consistency via cache invalidation strategies."}
EOF

mempalacenet mine ~/my-palace ~/my-convos.jsonl \
  --wing conversations \
  --mode convos
```

---

### Step 3: Search the Palace (Semantic Search)

Now let's retrieve memories using semantic search—it finds relevant content by meaning, not keywords.

#### Command

```bash
mempalacenet search "How do I secure user sessions?" \
  --wing documentation \
  --top-k 5
```

#### Expected Output

```
Searching wing 'documentation' for: "How do I secure user sessions?"

Results (by relevance):

1. [0.892] Use secure session cookies (httpOnly, Secure flags)
   └─ Chunk from: auth.md | Stored: 2024-12-05T14:32:01Z

2. [0.847] Always use HTTPS for auth endpoints
   └─ Chunk from: auth.md | Stored: 2024-12-05T14:32:01Z

3. [0.823] Hash passwords with bcrypt or Argon2
   └─ Chunk from: auth.md | Stored: 2024-12-05T14:32:01Z
```

**Understanding the Output:**

- **Score** (0–1): How relevant each result is to your query. Higher = more relevant
- **Content**: The actual memory text
- **Metadata**: Source file, timestamp

#### Try Other Searches

```bash
# Search all wings (not just documentation)
mempalacenet search "JWT tokens"

# Hybrid search (semantic + keyword matching)
mempalacenet search "OAuth" --hybrid

# Limit results
mempalacenet search "authentication" --top-k 3
```

---

### Step 4: Wake Up (Retrieve Recent Memories)

**Wake Up** loads a summary of your most recent activity—useful when starting a new session.

#### Command

```bash
mempalacenet wake-up
```

#### Expected Output

```
Recalling recent memories from your palace...

Recent Activity (Last 24 hours):
───────────────────────────────

Wing: documentation
  ✓ 3 new memories
  Last updated: 2024-12-05 14:32 UTC

Wing: conversations
  ✓ 2 new memories
  Last updated: 2024-12-05 15:00 UTC

Suggested next steps:
  - Search for "authentication" to explore recent additions
  - Review knowledge graph relationships
```

---

### Step 5: Knowledge Graph Operations (Optional)

The **knowledge graph** tracks relationships between entities over time (e.g., "Alice works on Project X from Q1 2024").

#### Add an Entity

```bash
mempalacenet kg add Alice engineer 2024-Q1
```

#### Add a Relationship

```bash
mempalacenet kg add Alice "works_on" "MemPalace.Core" \
  --valid-from 2024-01-01 \
  --valid-to 2024-06-30
```

#### Query Relationships

```bash
# Find what Alice worked on
mempalacenet kg query "Alice works_on ?"
```

Expected output:

```
Relationships matching pattern: Alice works_on ?

Results:
  Alice works_on MemPalace.Core (2024-01-01 to 2024-06-30)
  Alice works_on MemPalace.Ai (2024-01-15 to present)
```

#### View a Timeline

```bash
mempalacenet kg timeline Alice
```

---

### Step 6: Run an Agent (If Configured)

Agents are AI assistants with memory. If you've registered an `IChatClient` (OpenAI, Ollama, etc.), you can run agents.

#### List Available Agents

```bash
mempalacenet agents list
```

Expected output:

```
Available Agents:
  • scribe (in wing: assistants)
```

#### Run an Agent (One-Shot)

```bash
mempalacenet agents run scribe "What are the key authentication patterns?"
```

Expected output:

```
Agent: Scribe
───────────────────

Based on your palace knowledge, the key authentication patterns are:

1. JWT (JSON Web Tokens) - stateless tokens with encoded claims
2. OAuth 2.0 - industry standard for delegated access
3. Session cookies - secure, httpOnly-flagged cookies

The palace contains 3 related memories. See --verbose for details.

Tokens: input=120, output=85 | Latency: 1.2s
```

#### Chat with an Agent (Interactive)

```bash
mempalacenet agents chat scribe
```

```
Scribe (type 'exit' to quit):
> How does semantic search work in MemPalace?

Scribe: Semantic search uses ONNX embeddings to convert queries and documents
into high-dimensional vectors, then computes similarity using cosine distance.
This allows finding relevant memories by meaning, not just keywords...

> Tell me more about embeddings.

Scribe: Embeddings are dense vector representations of text...
[Continues conversation. Exit by typing 'exit' and pressing Enter]
```

**Note:** Agent chat requires an `IChatClient` to be registered. See [agents.md](../agents.md) for setup instructions.

---

## Troubleshooting

### "Command not found: mempalacenet"

**Solution:** Verify the tool installed correctly:

```bash
dotnet tool list -g | grep mempalacenet
```

If missing, reinstall:

```bash
dotnet tool install -g mempalacenet --version 0.6.0
```

On macOS/Linux, you may need to add `~/.dotnet/tools` to your PATH:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### "Permission denied" when accessing palace

**Solution:** Check directory permissions:

```bash
ls -ld ~/my-palace
```

Ensure you own the directory. Fix with:

```bash
chmod 755 ~/my-palace
```

### "Database locked" error

**Solution:** Ensure no other process is accessing the palace:

```bash
# Kill any stuck processes
pkill -f mempalacenet

# Wait a moment, then retry
mempalacenet search "test"
```

### "No results found" in search

**Possible causes:**

1. **Palace is empty** — Run `mempalacenet mine` first
2. **Embedder is initializing** — First search loads the ONNX model (~50 MB). Try again in 10 seconds
3. **Query is too specific** — Try broader terms or use `--hybrid` for keyword matching

**Solution:**

```bash
# Check palace status
ls -la ~/my-palace/palace.db

# Try a simpler search
mempalacenet search "documentation"

# Use hybrid search
mempalacenet search "your query" --hybrid
```

### "IChatClient not registered" when running agents

**Solution:** Agents require a chat client for AI inference. Register one in your code:

```csharp
services.AddChatClient(builder => builder
    .Use(new OpenAIChatClient("gpt-4", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))));
```

Or for CLI: configure `OPENAI_API_KEY` and the CLI will auto-register if available.

### Embedding model download fails

**Solution:** The first run downloads the ONNX embedding model (~50 MB). Ensure:

1. **Internet connectivity** — Required for first run only
2. **Disk space** — ~100 MB free in `~/.cache/mempalace-ai`
3. **Windows Defender/antivirus** — May block model download; add cache dir to exclusions

---

## Next Steps

Congratulations! You've completed the core MemPalace.NET workflow. Here's where to go next:

### 🎓 Learn More

- **[CLI Reference](../cli.md)** — All commands, options, and environment variables
- **[Semantic Search Deep Dive](../search.md)** — Query syntax, BM25, reranking
- **[Knowledge Graph Guide](../kg.md)** — Temporal relationships, queries, timelines
- **[Agent Framework](../agents.md)** — Build autonomous agents with tool access

### 🛠️ Integrate into Your App

- **[Library Developer Guide](guides/csharp-library-developers.md)** — Use MemPalace.NET in your .NET applications
- **[RAG Integration](../SKILL_PATTERNS.md#rag-context-injection)** — Inject memories into LLM prompts
- **[MCP Server](../mcp.md)** — Expose your palace to Claude Desktop and VS Code

### 📚 Examples

- **[Simple Memory Agent](../../examples/SimpleMemoryAgent/)** — Complete C# project with semantic search
- **[Semantic Knowledge Graph](../../examples/SemanticKnowledgeGraph/)** — Temporal relationships in action
- **[Custom Skills](../../examples/CustomSkillTemplate/)** — Extend MemPalace with domain-specific tools

### 🚀 Advanced Topics

- **[Custom Embedders](../cli-embedder-config.md)** — Swap ONNX for OpenAI, Azure, or Ollama
- **[Backend Migration](../backends.md)** — Scale to Qdrant, PostgreSQL, or other vector stores
- **[MCP Tool Catalog](../mcp-tools-catalog.md)** — All 7 MCP tools and their capabilities

---

## Tips for Success

1. **Start small:** Mine 1–2 documents first to understand the workflow
2. **Use descriptive wing names:** "documentation", "code", "research" work better than "wing1"
3. **Search semantically:** Formulate queries as natural questions, not keyword lists
4. **Organize by topic:** One wing per major area (work, personal, research)
5. **Check recent activity:** Run `mempalacenet wake-up` regularly to stay in sync

---

## Feedback & Support

- **Issues?** Report bugs on [GitHub](https://github.com/elbruno/mempalacenet/issues)
- **Questions?** Start a [Discussion](https://github.com/elbruno/mempalacenet/discussions)
- **Contributing?** See [CONTRIBUTING.md](../../CONTRIBUTING.md)

---

**Happy memory-making!** 🏛️✨
