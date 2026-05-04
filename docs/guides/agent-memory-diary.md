# Agent Memory Diary Guide

**Build long-lived agents with persistent memory** — Enable chatbots, research assistants, and code generators to remember past conversations and maintain coherent state across sessions.

**Read time:** ~7 minutes | **Difficulty:** Intermediate

---

## What is an Agent Diary?

An **agent diary** is a persistent memory store for AI agents. Each conversation turn (user message + assistant response) is recorded as a semantic memory, allowing the agent to:

- **Recall past decisions** ("What was I working on yesterday?")
- **Maintain context** across multi-turn conversations
- **Learn from history** (avoid repeating mistakes)
- **Provide coherent responses** grounded in prior interactions

---

## Use Cases

### ✅ **Best for:**

- **Long-lived chatbots** — Customer support agents with conversation history
- **Research assistants** — Track sources explored, insights gained
- **Code generators** — Remember user preferences, project context
- **Personal assistants** — Calendar, tasks, user habits over time

### ❌ **Not ideal for:**

- **Stateless Q&A** — No need for history if each query is independent
- **One-shot tasks** — Agents that run once and exit
- **High-frequency logging** — Don't store every trivial turn (filter important decisions)

---

## Architecture

Each agent uses **one wing per agent ID** in the palace:

```
wings/
  agents/
    scribe/        ← Agent "scribe" stores memories here
    assistant/     ← Agent "assistant" stores memories here
```

**Storage:**
- Each turn is stored as an embedded memory (content + metadata)
- Semantic search retrieves relevant past turns
- Memories are isolated per agent (no cross-contamination)

---

## Step-by-Step Walkthrough

### Prerequisites

- MemPalace.NET installed (`dotnet add package mempalacenet`)
- Familiarity with [Getting Started](GETTING_STARTED.md)
- Optional: An `IChatClient` for LLM integration (see [agents.md](../agents.md))

---

### Step 1: Create Palace for Agent State

```csharp
using MemPalace;
using MemPalace.Agents.Diary;
using MemPalace.Search;

// Initialize palace
var palace = await Palace.Create("~/agent-memory");

// Create diary for agent "scribe"
var agentId = "scribe";
var diary = new BackedByPalaceDiary(
    palace.Backend,
    palace.Embedder,
    new VectorSearchService(palace.Backend, palace.Embedder)
);
```

---

### Step 2: Store Memory at Each Conversation Turn

Each turn includes:
- **AgentId:** Unique agent identifier
- **Timestamp:** When the turn occurred
- **Role:** "user" or "assistant"
- **Content:** The message text
- **Metadata:** Optional context (turn number, topic, etc.)

```csharp
// Turn 1: User asks about authentication
var turn1 = new DiaryEntry(
    AgentId: agentId,
    At: DateTimeOffset.UtcNow,
    Role: "assistant",
    Content: "I'm implementing JWT auth validation. Need to handle token expiry and refresh cycles.",
    Metadata: new Dictionary<string, object?> { { "turn", 1 }, { "topic", "auth" } }
);

await diary.AppendAsync(agentId, turn1);

// Turn 2: Agent completes auth work
var turn2 = new DiaryEntry(
    AgentId: agentId,
    At: DateTimeOffset.UtcNow.AddMinutes(5),
    Role: "assistant",
    Content: "JWT validation complete. Now working on role-based access control (RBAC).",
    Metadata: new Dictionary<string, object?> { { "turn", 2 }, { "topic", "auth" } }
);

await diary.AppendAsync(agentId, turn2);

Console.WriteLine("✓ Stored 2 conversation turns in agent diary");
```

---

### Step 3: Search Diary for Historical Context

```csharp
// Agent needs to recall auth-related work
var authContext = await diary.SearchAsync(agentId, "auth context", topK: 5);

Console.WriteLine($"\nRecalled {authContext.Count} memories about auth:");
foreach (var memory in authContext)
{
    Console.WriteLine($"[{memory.At:yyyy-MM-dd HH:mm}] {memory.Content}");
}
```

**Expected output:**
```
Recalled 2 memories about auth:
[2024-12-05 14:32] I'm implementing JWT auth validation. Need to handle token expiry and refresh cycles.
[2024-12-05 14:37] JWT validation complete. Now working on role-based access control (RBAC).
```

---

### Step 4: Inject Retrieved Context into LLM Prompt

```csharp
using System.Text;

public static string FormatMemoriesForLLM(IReadOnlyList<DiaryEntry> memories)
{
    var sb = new StringBuilder();
    sb.AppendLine("=== Agent Context (Recent Memories) ===");
    
    for (int i = 0; i < memories.Count; i++)
    {
        var memory = memories[i];
        sb.AppendLine($"{i + 1}. [{memory.At:yyyy-MM-dd HH:mm}] {memory.Content}");
    }
    
    sb.AppendLine("=== End Context ===");
    return sb.ToString();
}

// Usage
var recentMemories = await diary.RecentAsync(agentId, take: 10);
var contextForPrompt = FormatMemoriesForLLM(recentMemories);

// Inject into LLM system prompt
var systemPrompt = $@"
You are a helpful assistant named Scribe. You help users with authentication and security.

{contextForPrompt}

Answer the user's question based on your past work shown above.
";

Console.WriteLine(systemPrompt);
```

**Expected output:**
```
You are a helpful assistant named Scribe. You help users with authentication and security.

=== Agent Context (Recent Memories) ===
1. [2024-12-05 14:32] I'm implementing JWT auth validation. Need to handle token expiry and refresh cycles.
2. [2024-12-05 14:37] JWT validation complete. Now working on role-based access control (RBAC).
=== End Context ===

Answer the user's question based on your past work shown above.
```

---

### Step 5: Verify Coherence (Agent Understands History)

```csharp
// Simulate agent query: "What was I working on?"
var query = "What was I working on recently?";
var relevantMemories = await diary.SearchAsync(agentId, query, topK: 5);

// Check recall quality
var expectedKeywords = new[] { "JWT", "auth", "RBAC" };
var recalledKeywords = relevantMemories
    .SelectMany(m => expectedKeywords.Where(k => m.Content.Contains(k)))
    .Distinct()
    .Count();

var recallRate = recalledKeywords / (double)expectedKeywords.Length;

Console.WriteLine($"\nRecall quality: {recallRate * 100:F1}% (recalled {recalledKeywords}/{expectedKeywords.Length} keywords)");

if (recallRate >= 0.80)
{
    Console.WriteLine("✓ Agent diary meets R@5 ≥80% SLO");
}
```

---

## Code Example: Multi-Turn Memory Pattern

```csharp
using MemPalace;
using MemPalace.Agents.Diary;
using MemPalace.Search;

public class AgentMemoryExample
{
    private readonly BackedByPalaceDiary _diary;
    private readonly string _agentId;

    public AgentMemoryExample(Palace palace, string agentId)
    {
        _diary = new BackedByPalaceDiary(
            palace.Backend,
            palace.Embedder,
            new VectorSearchService(palace.Backend, palace.Embedder)
        );
        _agentId = agentId;
    }

    public async Task Remember(string content, string role = "assistant")
    {
        var entry = new DiaryEntry(
            AgentId: _agentId,
            At: DateTimeOffset.UtcNow,
            Role: role,
            Content: content,
            Metadata: null
        );
        
        await _diary.AppendAsync(_agentId, entry);
        Console.WriteLine($"✓ Recorded: {content.Substring(0, Math.Min(50, content.Length))}...");
    }

    public async Task<List<DiaryEntry>> Recall(string query, int topK = 5)
    {
        var results = await _diary.SearchAsync(_agentId, query, topK);
        Console.WriteLine($"✓ Recalled {results.Count} memories for: {query}");
        return results;
    }

    public async Task<List<DiaryEntry>> GetRecentHistory(int count = 10)
    {
        return await _diary.RecentAsync(_agentId, take: count);
    }
}

// Usage
var palace = await Palace.Create("~/agent-demo");
var agent = new AgentMemoryExample(palace, "scribe");

// Store memories
await agent.Remember("User prefers async/await over Task.Result");
await agent.Remember("User's project uses .NET 8 with nullable types");
await agent.Remember("User's code style: 4 spaces, no tabs");

// Recall memories
var memories = await agent.Recall("code style preferences");
foreach (var memory in memories)
{
    Console.WriteLine($"  - {memory.Content}");
}
```

---

## Best Practices

### 1. **Store Decisions & Learnings, Not Every Turn**

❌ **Don't store:**
```
"User said hello"
"I replied hello"
"User asked about weather"
```

✅ **Do store:**
```
"User prefers detailed technical explanations over summaries"
"User is building a REST API with .NET 8 and PostgreSQL"
"User's security requirement: OAuth2 + JWT with 15-min expiry"
```

### 2. **Use Descriptive Summaries**

Store **summaries** of decisions, not raw transcripts:

```csharp
// ❌ Don't store raw transcript
await diary.AppendAsync(agentId, new DiaryEntry(
    agentId, DateTimeOffset.UtcNow, "user",
    "Hey can you help me with auth? I'm thinking JWT but not sure...",
    null
));

// ✅ Store decision summary
await diary.AppendAsync(agentId, new DiaryEntry(
    agentId, DateTimeOffset.UtcNow, "assistant",
    "Decision: Implement JWT auth with 15-min expiry and refresh tokens. User chose stateless over session-based.",
    new Dictionary<string, object?> { { "decision_type", "architecture" } }
));
```

### 3. **Periodically Review Diary for Contradictions**

```csharp
// Example: Detect conflicting preferences
var allMemories = await diary.RecentAsync(agentId, take: 100);
var hasJwt = allMemories.Any(m => m.Content.Contains("JWT"));
var hasSessionAuth = allMemories.Any(m => m.Content.Contains("session-based auth"));

if (hasJwt && hasSessionAuth)
{
    Console.WriteLine("⚠️ Warning: Diary contains conflicting auth strategies");
}
```

### 4. **Archive Old Entries After N Months**

```csharp
// Pseudo-code: Archive entries older than 6 months
var cutoff = DateTimeOffset.UtcNow.AddMonths(-6);
var oldEntries = allMemories.Where(m => m.At < cutoff);

// Move to archive wing or delete
foreach (var entry in oldEntries)
{
    // Archive or prune logic here
}
```

---

## Performance SLOs

| Metric | Target | Notes |
|--------|--------|-------|
| **Memory retrieval** | <50ms | Search 5 memories from diary |
| **Recall@5** | ≥80% | Top-5 results contain relevant memories |
| **Storage latency** | <100ms | Append new diary entry |
| **Coherence** | 100% | Agent understands its own history |

---

## ⚠️ Common Pitfalls

### 1. Storing Too Many Memories

**Problem:** Diary grows to 10,000+ entries; search becomes slow.  
**Solution:** Store only important turns. Filter trivial greetings, confirmations, etc.

### 2. Not Using Metadata

**Problem:** Can't filter by date, topic, or turn type.  
**Solution:** Add metadata to each entry:

```csharp
Metadata: new Dictionary<string, object?>
{
    { "turn", 42 },
    { "topic", "authentication" },
    { "importance", "high" }
}
```

### 3. Cross-Agent Contamination

**Problem:** Agent A sees Agent B's memories.  
**Solution:** Always scope search by `agentId`:

```csharp
// ✓ Correct: agent-specific search
var memories = await diary.SearchAsync("scribe", query, topK: 5);

// ✗ Wrong: searches all agents
var memories = await diary.SearchAsync(null, query, topK: 5);
```

---

## See Also

- **[MultiAgentMemoryTests.cs](../../src/MemPalace.E2E.Tests/MultiAgentMemoryTests.cs)** — Full E2E test code
- **[agents.md](../agents.md)** — Agent framework documentation
- **[SKILL_PATTERNS.md](../SKILL_PATTERNS.md)** — Pattern 4: Agent Memory
- **[RAG Integration Guide](rag-integration-guide.md)** — Use diary memories in RAG pipelines

---

**Next steps:** Build your own agent diary, experiment with memory storage strategies, and measure recall quality. 🤖📖
