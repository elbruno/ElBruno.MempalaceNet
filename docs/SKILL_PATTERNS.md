# MemPalace.NET — Copilot Skill Patterns

This document contains **high-value teaching patterns** for integrating MemPalace.NET into your AI applications. Each pattern includes a description, code example, use case guidance, and **performance recommendations** (new in v0.7.0).

---

## Pattern 1: Wake-Up Summarization for Session Initialization

### Description
Use **wake-up with AI-powered summarization** to initialize agent sessions with recent context. This pattern demonstrates:
1. Retrieve recent memories from a wing/collection
2. Generate an AI summary of recent activity
3. Inject the summary into agent initialization
4. Reduce token usage by summarizing instead of passing raw memories

### Code Example

```csharp
using MemPalace;
using MemPalace.Core.Services;
using Microsoft.Extensions.AI;

public class SessionInitializer
{
    private readonly Palace _palace;
    private readonly IWakeUpService _wakeUpService;
    private readonly IChatClient _chatClient;

    public SessionInitializer(Palace palace, IWakeUpService wakeUpService, IChatClient chatClient)
    {
        _palace = palace;
        _wakeUpService = wakeUpService;
        _chatClient = chatClient;
    }

    // Initialize a new session with recent context summary
    public async Task<string> InitializeSession(string wing = "conversations", int limit = 20)
    {
        // Get collection
        var collection = await _palace.GetCollection(wing);
        
        // Wake up with summarization
        var wakeUpResult = await _wakeUpService.WakeUpAsync(
            collection: collection,
            limit: limit,
            where: null, // or use wing filter: new Eq("wing", wing)
            summarize: true
        );

        if (wakeUpResult.Summary != null)
        {
            return wakeUpResult.Summary;
        }

        // Fallback if summarization is unavailable
        return BuildFallbackSummary(wakeUpResult.Memories);
    }

    // Use summary to initialize agent with context
    public async Task<string> StartConversation(string userQuestion, string wing = "conversations")
    {
        // Get session context via wake-up
        var contextSummary = await InitializeSession(wing, limit: 30);

        // Inject into system prompt
        var systemPrompt = $@"
You are a helpful AI assistant with access to recent conversation history.

RECENT ACTIVITY SUMMARY:
{contextSummary}

Use this context to provide informed, personalized responses.
";

        // Create conversation with context
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userQuestion)
        };

        var response = await _chatClient.CompleteAsync(messages);
        return response.Message.Text;
    }

    private static string BuildFallbackSummary(IReadOnlyList<EmbeddedRecord> memories)
    {
        // Group by wing for structured summary
        var wingGroups = memories
            .GroupBy(m => m.Metadata.TryGetValue("wing", out var w) ? w?.ToString() : "general")
            .Take(3);

        var summary = $"Recent activity ({memories.Count} memories):\n";
        foreach (var group in wingGroups)
        {
            var recent = group.Take(2).Select(m => 
                m.Document.Length > 60 ? m.Document.Substring(0, 57) + "..." : m.Document
            );
            summary += $"\n• {group.Key}: {string.Join("; ", recent)}";
        }
        return summary;
    }
}

// Usage
var palace = await Palace.Create("~/my-palace");
var chatClient = new OpenAIChatClient("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
var wakeUpService = new WakeUpService(chatClient);
var initializer = new SessionInitializer(palace, wakeUpService, chatClient);

// Start a conversation with recent context
var response = await initializer.StartConversation(
    "What have we been discussing about the authentication module?",
    wing: "engineering"
);
Console.WriteLine(response);
```

### CLI Usage

```bash
# Wake up with automatic summarization
mempalacenet wake-up --wing conversations --limit 30 --summarize

# Output includes:
# 1. AI-generated summary (2-3 paragraphs)
# 2. Tree view of recent memories
# 3. Statistics (total memories, wings, last activity)
```

### Use Cases
- **Agent Session Initialization:** Provide agents with recent context without overwhelming token limits
- **Daily Briefings:** Generate summaries of yesterday's work or conversations
- **Context Compression:** Reduce 50+ memories to a 100-word summary for efficient prompting
- **Multi-Agent Handoff:** Summarize Agent A's conversation for Agent B to continue

### Best Practices
- **Set appropriate limits:** 20-50 memories for balanced context vs. summarization time
- **Use wing filtering:** Scope wake-up to relevant categories (`work`, `personal`, `bugs`)
- **Cache summaries:** Store generated summaries as memories to avoid redundant API calls
- **Combine with semantic search:** Use wake-up for chronological context, search for topical context
- **Monitor token usage:** Summarization reduces prompt tokens by ~70-80%

### Performance Recommendations
- **Wake-up query (no summary):** ~50-100ms for 20 memories (SQLite backend)
- **Wake-up with summarization:** ~1-2s (GPT-4o-mini), ~0.5s (local LLM like Llama 3)
- **Token savings:** 50 raw memories (~5000 tokens) → summary (~500 tokens) = 90% reduction
- **Optimal limit:** 20-30 memories balances context richness with summarization speed

---

## Pattern 2: Semantic Search for Context Injection

### Description
Use MemPalace.NET as a **RAG (Retrieval-Augmented Generation)** source to inject relevant context into LLM prompts. This pattern demonstrates how to:
1. Store documents/conversations in a palace
2. Perform semantic search to retrieve relevant memories
3. Inject retrieved context into an LLM prompt
4. Generate a contextually-aware response

### Code Example

```csharp
using MemPalace;
using Microsoft.Extensions.AI;

public class RagContextInjector
{
    private readonly Palace _palace;
    private readonly IChatClient _chatClient;

    public RagContextInjector(Palace palace, IChatClient chatClient)
    {
        _palace = palace;
        _chatClient = chatClient;
    }

    public async Task<string> AnswerWithContext(string question, string wing, int contextLimit = 3)
    {
        // 1. Retrieve relevant context using semantic search
        var contextResults = await _palace.Search(
            query: question,
            wing: wing,
            limit: contextLimit
        );

        // 2. Build context string from retrieved memories
        var context = string.Join("\n\n---\n\n", 
            contextResults.Select(r => 
                $"[Score: {r.Score:F3}]\n{r.Memory.Content}"
            )
        );

        // 3. Inject context into LLM prompt
        var prompt = $@"
You are a helpful assistant. Use the following context to answer the question.

CONTEXT:
{context}

QUESTION:
{question}

ANSWER (be concise and cite sources if possible):
";

        // 4. Generate response
        var response = await _chatClient.CompleteAsync(prompt);
        return response.Message.Text;
    }
}

// Usage
var palace = await Palace.Create("~/my-palace");
var chatClient = new OpenAIChatClient("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
var injector = new RagContextInjector(palace, chatClient);

var answer = await injector.AnswerWithContext(
    question: "How do I implement OAuth2 authentication?",
    wing: "documentation"
);
Console.WriteLine(answer);
```

### Use Cases
- **Documentation Q&A:** Index your codebase documentation and answer developer questions
- **Customer Support:** Retrieve relevant FAQs/tickets to assist support agents
- **Research Assistant:** Search academic papers or notes for relevant citations
- **Code Review Context:** Pull related code changes or discussions for PR reviews

### Best Practices
- **Set appropriate `contextLimit`:** Too few results miss context; too many overwhelm the LLM
- **Use `wing` scoping:** Organize memories by topic (e.g., "docs", "tickets", "conversations")
- **Consider hybrid search:** Use `SearchMode.Hybrid` for better precision with keyword matching
- **Rerank results:** Use `--rerank` (CLI) or implement LLM-based reranking for top results

### Performance Recommendations
- **Semantic search (3 results):** ~100-200ms (SQLite + ONNX embeddings)
- **Semantic search (10 results):** ~200-400ms
- **Hybrid search:** +50-100ms overhead (FTS5 query + merge)
- **LLM completion:** ~1-3s (depends on model: GPT-4o ~2s, GPT-4o-mini ~1s)
- **End-to-end RAG:** ~1.5-4s (search + LLM generation)
- **Token usage:** 3 contexts (~1500 tokens) + question (~50 tokens) + system (~100 tokens) ≈ 1650 input tokens

---

## Pattern 2: Agent Diaries for State Persistence

### Description
Give each AI agent its own **memory diary** (a dedicated wing in the palace) to persist state across sessions. This pattern enables:
- Multi-turn conversation context tracking
- Long-term memory for agents
- Semantic retrieval of past interactions
- Integration with Microsoft Agent Framework
- **Wake-up with summarization** for session initialization

### Code Example

```csharp
using MemPalace;
using MemPalace.Core.Services;
using Microsoft.Extensions.AI;

public class AgentDiary
{
    private readonly Palace _palace;
    private readonly IWakeUpService _wakeUpService;
    private readonly string _agentId;
    private readonly string _wing;

    public AgentDiary(Palace palace, IWakeUpService wakeUpService, string agentId)
    {
        _palace = palace;
        _wakeUpService = wakeUpService;
        _agentId = agentId;
        _wing = $"agents/{agentId}";
    }

    // Store an interaction
    public async Task RecordInteraction(string userMessage, string agentResponse, string? userId = null)
    {
        var timestamp = DateTime.UtcNow;
        var content = $@"
[{timestamp:yyyy-MM-dd HH:mm:ss} UTC]
User: {userMessage}
Agent: {agentResponse}
";

        await _palace.Store(
            content: content,
            metadata: new Dictionary<string, object>
            {
                { "agentId", _agentId },
                { "userId", userId ?? "anonymous" },
                { "timestamp", timestamp.ToString("o") },
                { "userMessage", userMessage },
                { "agentResponse", agentResponse }
            },
            wing: _wing
        );
    }

    // Recall past interactions
    public async Task<List<QueryResult>> RecallContext(string query, int limit = 5)
    {
        return await _palace.Search(
            query: query,
            wing: _wing,
            limit: limit
        );
    }

    // Get full conversation history
    public async Task<List<Memory>> GetHistory(int limit = 50)
    {
        var results = await _palace.WakeUp(
            wing: _wing,
            limit: limit
        );
        return results.OrderBy(m => m.Metadata["timestamp"]).ToList();
    }

    // Wake up with automatic summarization (NEW in v0.7.0)
    public async Task<WakeUpResult> WakeUpWithSummary(int limit = 20)
    {
        var collection = await _palace.GetCollection(_wing);
        var whereClause = new Eq("wing", _wing);
        
        return await _wakeUpService.WakeUpAsync(
            collection: collection,
            limit: limit,
            where: whereClause,
            summarize: true
        );
    }

    // Initialize agent session with recent context
    public async Task<string> InitializeSession(IChatClient chatClient)
    {
        var wakeUpResult = await WakeUpWithSummary(limit: 20);
        
        if (wakeUpResult.Summary != null)
        {
            // Use the AI-generated summary
            return wakeUpResult.Summary;
        }
        
        // Fallback: build context from recent memories
        var recentContext = string.Join("\n", 
            wakeUpResult.Memories.Take(5).Select(m => $"- {m.Document}")
        );
        
        return $"Recent activity:\n{recentContext}";
    }

    // Summarize past interactions
    public async Task<string> SummarizePastInteractions(IChatClient chatClient, string topic)
    {
        var context = await RecallContext(topic, limit: 10);
        var memories = string.Join("\n\n", context.Select(r => r.Memory.Content));
        
        var prompt = $@"
Summarize the following past interactions related to '{topic}':

{memories}

Summary (2-3 sentences):
";

        var response = await chatClient.CompleteAsync(prompt);
        return response.Message.Text;
    }
}

// Usage
var palace = await Palace.Create("~/my-palace");
var wakeUpService = new WakeUpService(chatClient);
var diary = new AgentDiary(palace, wakeUpService, agentId: "customer-support-bot-42");

// Initialize session with wake-up summary
var sessionContext = await diary.InitializeSession(chatClient);
Console.WriteLine($"Session context: {sessionContext}");

// Record an interaction
await diary.RecordInteraction(
    userMessage: "What's your refund policy?",
    agentResponse: "Our refund policy allows returns within 30 days. See https://example.com/refunds",
    userId: "user-12345"
);

// Later, recall context
var pastDiscussions = await diary.RecallContext("refund policy", limit: 5);
foreach (var result in pastDiscussions)
{
    Console.WriteLine($"[{result.Score:F3}] {result.Memory.Content}");
}
```

### Use Cases
- **Customer Support Bots:** Remember past user interactions for personalized responses
- **Coding Assistants:** Track which files/functions the user has asked about
- **Research Agents:** Store findings and hypotheses across research sessions
- **Conversational AI:** Maintain long-term memory for chatbots
- **Session Initialization:** Wake up with summarization to provide agents with recent context

### Best Practices
- **Use agent-specific wings:** `agents/{agentId}` ensures isolation between agents
- **Store structured metadata:** Include `userId`, `timestamp`, `sessionId` for filtering
- **Implement periodic cleanup:** Remove old/irrelevant memories to reduce noise
- **Use wake-up summarization:** Initialize sessions with AI-generated summaries for efficiency
- **Set appropriate limits:** 20-50 recent memories for wake-up, 5-10 for targeted recall

### Performance Recommendations
- **Wake-up queries:** ~50-100ms for 20 memories (SQLite backend)
- **Summarization:** ~1-2s with GPT-4o-mini, ~0.5s with local LLM
- **Memory limit:** Keep limit ≤50 for wake-up to avoid long summarization times
- **Indexing:** Ensure `timestamp` field is indexed for fast retrieval

---

## Pattern 3: Knowledge Graph Queries

### Description
Use the **temporal knowledge graph** to track entity relationships with validity windows. This pattern enables:
- Temporal queries ("Who was the CEO in 2019?")
- Relationship traversal ("What projects does Alice work on?")
- Historical snapshots ("What was the team structure in Q1 2023?")

### Code Example

```csharp
using MemPalace;

public class TeamKnowledgeGraph
{
    private readonly Palace _palace;

    public TeamKnowledgeGraph(Palace palace)
    {
        _palace = palace;
    }

    // Add a team member
    public async Task AddTeamMember(string employeeId, string name, string role, DateTime joinDate)
    {
        await _palace.KnowledgeGraph.AddEntity(
            entityId: employeeId,
            entityType: "person",
            properties: new { name, role, joinDate }
        );
    }

    // Assign to a project
    public async Task AssignToProject(string employeeId, string projectId, DateTime startDate, DateTime? endDate = null)
    {
        await _palace.KnowledgeGraph.AddRelationship(
            fromId: employeeId,
            toId: projectId,
            relationshipType: "works_on",
            validFrom: startDate,
            validTo: endDate
        );
    }

    // Query current projects for an employee
    public async Task<List<string>> GetCurrentProjects(string employeeId)
    {
        var relationships = await _palace.KnowledgeGraph.Query(
            entityId: employeeId,
            relationshipType: "works_on",
            asOf: DateTime.UtcNow
        );

        return relationships
            .Where(r => r.ValidTo == null || r.ValidTo > DateTime.UtcNow)
            .Select(r => r.ToEntityId)
            .ToList();
    }

    // Query historical team assignments
    public async Task<List<string>> GetProjectTeam(string projectId, DateTime asOf)
    {
        var relationships = await _palace.KnowledgeGraph.Query(
            entityId: projectId,
            relationshipType: "works_on",
            asOf: asOf,
            direction: RelationshipDirection.Incoming
        );

        return relationships
            .Select(r => r.FromEntityId)
            .ToList();
    }

    // Track role changes
    public async Task UpdateRole(string employeeId, string newRole, DateTime effectiveDate)
    {
        // Fetch current entity
        var entity = await _palace.KnowledgeGraph.GetEntity(employeeId);
        
        // Store old role as a relationship (optional)
        var oldRole = entity.Properties["role"];
        await _palace.KnowledgeGraph.AddRelationship(
            fromId: employeeId,
            toId: $"role:{oldRole}",
            relationshipType: "had_role",
            validFrom: (DateTime)entity.Properties["joinDate"],
            validTo: effectiveDate
        );

        // Update entity with new role
        entity.Properties["role"] = newRole;
        await _palace.KnowledgeGraph.UpdateEntity(entity);
    }
}

// Usage
var palace = await Palace.Create("~/my-palace");
var kg = new TeamKnowledgeGraph(palace);

// Add team members
await kg.AddTeamMember("alice", "Alice Smith", "Senior Engineer", new DateTime(2023, 1, 15));
await kg.AddTeamMember("bob", "Bob Jones", "Product Manager", new DateTime(2022, 6, 1));

// Assign to projects
await kg.AssignToProject("alice", "project-x", new DateTime(2023, 2, 1));
await kg.AssignToProject("bob", "project-x", new DateTime(2023, 2, 1), new DateTime(2023, 12, 31));

// Query
var aliceProjects = await kg.GetCurrentProjects("alice");
Console.WriteLine($"Alice is currently working on: {string.Join(", ", aliceProjects)}");

var projectXTeamIn2023 = await kg.GetProjectTeam("project-x", new DateTime(2023, 6, 1));
Console.WriteLine($"Project X team in June 2023: {string.Join(", ", projectXTeamIn2023)}");
```

### Use Cases
- **Organizational Charts:** Track reporting structures and role changes over time
- **Project Management:** Model project assignments, dependencies, and timelines
- **Research Graphs:** Link papers, authors, citations, and topics
- **Supply Chain:** Track product sourcing, suppliers, and contract periods

### Best Practices
- **Use meaningful entity IDs:** Prefer `alice` over `emp-001` for readability
- **Model validity carefully:** Set `validTo = null` for ongoing relationships
- **Query with `asOf`:** Always specify temporal queries to get accurate snapshots
- **Traverse bidirectionally:** Use `direction: Incoming` to reverse relationship queries

### Performance Recommendations
- **Entity lookup:** ~10-20ms (SQLite indexed by entity ID)
- **Relationship query (no temporal filter):** ~20-50ms
- **Relationship query (with `asOf`):** ~30-60ms (temporal index scan)
- **Timeline query:** ~50-100ms (depends on entity activity)
- **Batch entity creation (100 entities):** ~500ms-1s
- **Batch relationship creation (100 triples):** ~300-800ms
- **Graph traversal (3 hops):** ~100-300ms (depends on fanout)

---

## Pattern 4: MCP Write Operations for AI Assistants

### Description
Use **MCP (Model Context Protocol) write tools** to enable AI assistants (Claude Desktop, VS Code Copilot) to directly modify your palace. This pattern demonstrates:
- Safe write operations with user confirmation
- Batch storage for efficiency
- Knowledge graph mutations
- Collection management

### Code Example

#### Palace Write Operations

```csharp
using MemPalace.Mcp;
using ModelContextProtocol;

// MCP tools are exposed via the MemPalace MCP server
// Clients can call these tools to perform write operations

// Example: Store a single memory via MCP
// Tool: palace_store
var storeRequest = new ToolCallRequest
{
    Name = "palace_store",
    Arguments = new Dictionary<string, object>
    {
        ["content"] = "User reported critical bug in authentication module. Stack trace indicates JWT token validation failure.",
        ["wing"] = "bugs",
        ["room"] = "critical",
        ["metadata"] = new Dictionary<string, object>
        {
            ["severity"] = "critical",
            ["component"] = "auth",
            ["reporter"] = "user-42"
        }
    }
};
// Returns: { "id": "mem_xyz789", "stored": true }

// Example: Batch store multiple memories
// Tool: palace_batch_store
var batchRequest = new ToolCallRequest
{
    Name = "palace_batch_store",
    Arguments = new Dictionary<string, object>
    {
        ["memories"] = new[]
        {
            new
            {
                content = "Implemented OAuth2 refresh token rotation",
                wing = "work",
                metadata = new { sprint = "sprint-24", story = "AUTH-42" }
            },
            new
            {
                content = "Fixed SQL injection vulnerability in search endpoint",
                wing = "security",
                metadata = new { cve = "CVE-2025-1234", severity = "high" }
            }
        }
    }
};
// Returns: { "stored": 2, "ids": ["mem_001", "mem_002"] }

// Example: Update an existing memory
// Tool: palace_update
var updateRequest = new ToolCallRequest
{
    Name = "palace_update",
    Arguments = new Dictionary<string, object>
    {
        ["id"] = "mem_abc123",
        ["metadata"] = new Dictionary<string, object>
        {
            ["status"] = "resolved",
            ["resolved_by"] = "alice",
            ["resolved_at"] = DateTime.UtcNow.ToString("o")
        }
    }
};
// Returns: { "id": "mem_abc123", "updated": true }

// Example: Delete a memory
// Tool: palace_delete
var deleteRequest = new ToolCallRequest
{
    Name = "palace_delete",
    Arguments = new Dictionary<string, object>
    {
        ["id"] = "mem_obsolete_123"
    }
};
// Returns: { "id": "mem_obsolete_123", "deleted": true }
```

#### Knowledge Graph Write Operations

```csharp
// Example: Add an entity to the knowledge graph
// Tool: kg_add_entity
var addEntityRequest = new ToolCallRequest
{
    Name = "kg_add_entity",
    Arguments = new Dictionary<string, object>
    {
        ["entityId"] = "person:alice",
        ["entityType"] = "person",
        ["properties"] = new Dictionary<string, object>
        {
            ["name"] = "Alice Smith",
            ["role"] = "Senior Engineer",
            ["email"] = "alice@example.com",
            ["team"] = "platform"
        }
    }
};
// Returns: { "entityId": "person:alice", "created": true }

// Example: Add a temporal relationship
// Tool: kg_add_relationship
var addRelRequest = new ToolCallRequest
{
    Name = "kg_add_relationship",
    Arguments = new Dictionary<string, object>
    {
        ["subject"] = "person:alice",
        ["predicate"] = "works-on",
        ["object"] = "project:mempalacenet",
        ["validFrom"] = "2024-01-01T00:00:00Z",
        ["validTo"] = null // ongoing
    }
};
// Returns: { "subject": "person:alice", "predicate": "works-on", "object": "project:mempalacenet", "created": true }

// Example: Track role changes over time
var addPastRoleRequest = new ToolCallRequest
{
    Name = "kg_add_relationship",
    Arguments = new Dictionary<string, object>
    {
        ["subject"] = "person:alice",
        ["predicate"] = "has-role",
        ["object"] = "role:engineer",
        ["validFrom"] = "2020-01-01T00:00:00Z",
        ["validTo"] = "2023-12-31T23:59:59Z" // ended
    }
};

var addCurrentRoleRequest = new ToolCallRequest
{
    Name = "kg_add_relationship",
    Arguments = new Dictionary<string, object>
    {
        ["subject"] = "person:alice",
        ["predicate"] = "has-role",
        ["object"] = "role:senior-engineer",
        ["validFrom"] = "2024-01-01T00:00:00Z",
        ["validTo"] = null // current
    }
};
```

### Claude Desktop Integration

```json
{
  "mcpServers": {
    "mempalace": {
      "command": "mempalacenet",
      "args": ["mcp"]
    }
  }
}
```

**Example conversation with Claude:**

```
User: "Add a memory about the security fix I just deployed"

Claude: I'll store that memory for you using the palace_store tool.
[Calls palace_store with user confirmation]

✓ Stored memory (ID: mem_xyz789) in wing 'security'

User: "Create a knowledge graph entity for our new team member Bob"

Claude: I'll add Bob to the knowledge graph.
[Calls kg_add_entity with user confirmation]

✓ Created entity person:bob with role 'Junior Developer'
```

### Use Cases
- **Interactive Memory Creation:** Let AI assistants store conversation insights automatically
- **Bug Tracking:** Claude can store bug reports with metadata during debugging sessions
- **Team Knowledge Management:** Build knowledge graphs conversationally with AI assistance
- **Documentation Mining:** AI can extract and store key facts from documents
- **Incident Response:** Store incident details and relationships during troubleshooting

### Best Practices
- **User confirmation is required:** All write operations prompt for user approval (security feature)
- **Use batch operations:** `palace_batch_store` is 10x faster than individual stores
- **Validate temporal ranges:** Ensure `validFrom` ≤ `validTo` for relationships
- **Use meaningful entity IDs:** Prefer `person:alice` over `e001` for readability
- **Store rich metadata:** Include `source`, `timestamp`, `author` for traceability

### Performance Recommendations
- **Single store:** ~100-200ms (includes embedding generation)
- **Batch store (10 items):** ~500ms (parallelized embeddings)
- **KG entity creation:** ~50ms
- **KG relationship creation:** ~30ms
- **MCP overhead:** ~20-50ms per tool call (JSON-RPC + stdio)

### Security Considerations
- **User confirmation required:** MCP clients (Claude, VS Code) prompt before writes
- **Read-only by default:** Use `palace_search`, `kg_query` for safe exploration
- **Audit trail:** Store `source: "mcp-tool"` in metadata for tracking
- **No API key exposure:** Embedder API keys are never sent to MCP clients

---

## Pattern 5: Local-First Privacy

### Description
Run MemPalace.NET entirely **locally** without external API calls using ONNX embeddings. This pattern ensures:
- No data leaves your machine
- No API keys required
- Zero per-token costs
- Works offline

### Code Example

```csharp
using MemPalace;
using ElBruno.LocalEmbeddings;

public class LocalFirstPalace
{
    public static async Task<Palace> CreateLocalPalace(string palacePath)
    {
        // Use ONNX embeddings (default, no API keys required)
        var config = new PalaceConfig
        {
            Path = palacePath,
            EmbedderType = EmbedderType.Local, // Uses ElBruno.LocalEmbeddings
            Backend = BackendType.Sqlite
        };

        return await Palace.Create(config);
    }

    public static async Task DemoLocalPrivacy()
    {
        // Initialize palace with local embeddings
        var palace = await CreateLocalPalace("~/private-palace");

        // Store sensitive data (never leaves your machine)
        await palace.Store(
            content: "Patient X diagnosed with condition Y on 2024-01-15",
            metadata: new Dictionary<string, object>
            {
                { "source", "medical-records" },
                { "classification", "confidential" }
            },
            wing: "medical"
        );

        // Semantic search (all processing local)
        var results = await palace.Search(
            query: "recent diagnoses",
            wing: "medical",
            limit: 5
        );

        // Results are computed locally using ONNX
        foreach (var result in results)
        {
            Console.WriteLine($"[{result.Score:F3}] {result.Memory.Content}");
        }

        Console.WriteLine("\n✅ No data sent to external APIs!");
    }
}

// Usage
await LocalFirstPalace.DemoLocalPrivacy();
```

### Use Cases
- **Healthcare:** Store patient records without HIPAA compliance concerns
- **Legal:** Index case files and contracts locally
- **Enterprise:** Keep proprietary data on-premises
- **Personal Notes:** Build a private knowledge base for journaling or research

### Best Practices
- **Use ONNX embeddings by default:** No configuration needed; works out of the box
- **Verify isolation:** Run `netstat` to confirm no outbound connections during operations
- **Benchmark performance:** ONNX is fast but may be slower than cloud embeddings for large batches
- **Consider hybrid:** Use local for sensitive data, cloud for non-sensitive high-volume workloads

### Performance Recommendations
- **ONNX embedding generation:** ~20-50ms per document (all-MiniLM-L6-v2)
- **Batch embedding (100 docs):** ~2-5s (parallelized)
- **Search with ONNX:** Same performance as cloud embeddings once embeddings are stored
- **Memory usage:** ~500MB for ONNX model (loaded once at startup)
- **Disk space:** ~90MB for all-MiniLM-L6-v2 model files

---

## Pattern 6: Hybrid Search with Reranking

### Description
Combine **semantic search** (vector similarity) with **keyword search** (SQL FTS5) and **LLM-based reranking** for maximum precision. This pattern is ideal when you need highly relevant results for complex queries.

### Code Example

```csharp
using MemPalace;
using Microsoft.Extensions.AI;

public class HybridSearchWithReranking
{
    private readonly Palace _palace;
    private readonly IChatClient _reranker;

    public HybridSearchWithReranking(Palace palace, IChatClient reranker)
    {
        _palace = palace;
        _reranker = reranker;
    }

    public async Task<List<QueryResult>> Search(string query, string wing, int candidateLimit = 20, int finalLimit = 5)
    {
        // Step 1: Hybrid search (semantic + keyword)
        var candidates = await _palace.Search(
            query: query,
            wing: wing,
            limit: candidateLimit,
            mode: SearchMode.Hybrid // Combines vector + FTS5
        );

        // Step 2: Rerank using LLM
        var reranked = await RerankResults(query, candidates, finalLimit);

        return reranked;
    }

    private async Task<List<QueryResult>> RerankResults(string query, List<QueryResult> candidates, int topK)
    {
        // Build reranking prompt
        var candidateTexts = candidates
            .Select((r, i) => $"[{i}] {r.Memory.Content}")
            .ToList();

        var prompt = $@"
You are a relevance judge. Given a query and a list of candidate documents, 
rank them by relevance to the query. Return only the indices of the top {topK} 
documents in order of relevance (most relevant first).

QUERY:
{query}

CANDIDATES:
{string.Join("\n\n", candidateTexts)}

OUTPUT FORMAT (comma-separated indices, e.g., '3,7,1,9,4'):
";

        var response = await _reranker.CompleteAsync(prompt);
        var rankedIndices = response.Message.Text
            .Split(',')
            .Select(s => int.Parse(s.Trim()))
            .ToList();

        // Reorder candidates by LLM ranking
        return rankedIndices
            .Take(topK)
            .Select(i => candidates[i])
            .ToList();
    }
}

// Usage
var palace = await Palace.Create("~/my-palace");
var reranker = new OpenAIChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
var search = new HybridSearchWithReranking(palace, reranker);

var results = await search.Search(
    query: "best practices for React hooks",
    wing: "frontend-docs",
    candidateLimit: 20,
    finalLimit: 5
);

foreach (var result in results)
{
    Console.WriteLine($"[{result.Score:F3}] {result.Memory.Content}");
}
```

### CLI Shortcut

```bash
# Hybrid search with reranking (one command)
mempalacenet search "React hooks best practices" \
  --hybrid \
  --rerank \
  --wing frontend-docs \
  --limit 5
```

### Use Cases
- **Documentation Search:** High-precision retrieval for developer questions
- **Legal Discovery:** Find relevant case law or contracts
- **Research Papers:** Retrieve citations matching complex queries
- **Code Search:** Find relevant code snippets across large codebases

### Best Practices
- **Increase `candidateLimit`:** Start with 20–50 candidates before reranking
- **Use cheaper LLMs for reranking:** GPT-4o-mini or Claude Haiku are sufficient
- **Cache reranking results:** Avoid redundant LLM calls for identical queries
- **Measure precision:** Compare hybrid+rerank vs. semantic-only for your use case

### Performance Recommendations
- **Hybrid search (20 candidates):** ~200-400ms (semantic + FTS5)
- **LLM reranking (20→5):** ~1-2s (GPT-4o-mini), ~0.5s (Claude Haiku)
- **End-to-end (hybrid + rerank):** ~1.5-2.5s total
- **Token usage:** ~1000 input tokens (20 candidates + prompt) + ~50 output tokens
- **Precision improvement:** ~15-25% better relevance vs. semantic-only search

---

## Next Steps

- **Try the examples:** See [examples/README.md](../examples/README.md) for runnable code
- **Read the docs:** [docs/COPILOT_SKILL.md](./COPILOT_SKILL.md) for full integration guide
- **Explore the CLI:** [docs/cli.md](./cli.md) for command-line workflows
- **Check the roadmap:** [docs/PLAN.md](./PLAN.md) for upcoming features

---

## License

MIT License — see [LICENSE](../LICENSE) for details.
