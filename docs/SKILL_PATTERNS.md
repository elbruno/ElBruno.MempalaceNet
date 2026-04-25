# MemPalace.NET — Copilot Skill Patterns

This document contains **high-value teaching patterns** for integrating MemPalace.NET into your AI applications. Each pattern includes a description, code example, and use case guidance.

---

## Pattern 1: Semantic Search for Context Injection

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

---

## Pattern 2: Agent Diaries for State Persistence

### Description
Give each AI agent its own **memory diary** (a dedicated wing in the palace) to persist state across sessions. This pattern enables:
- Multi-turn conversation context tracking
- Long-term memory for agents
- Semantic retrieval of past interactions
- Integration with Microsoft Agent Framework

### Code Example

```csharp
using MemPalace;
using Microsoft.Extensions.AI;

public class AgentDiary
{
    private readonly Palace _palace;
    private readonly string _agentId;
    private readonly string _wing;

    public AgentDiary(Palace palace, string agentId)
    {
        _palace = palace;
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
var diary = new AgentDiary(palace, agentId: "customer-support-bot-42");

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

### Best Practices
- **Use agent-specific wings:** `agents/{agentId}` ensures isolation between agents
- **Store structured metadata:** Include `userId`, `timestamp`, `sessionId` for filtering
- **Implement periodic cleanup:** Remove old/irrelevant memories to reduce noise
- **Summarize periodically:** Use LLMs to condense old interactions into summaries

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

---

## Pattern 4: Local-First Privacy

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

---

## Pattern 5: Hybrid Search with Reranking

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

---

## Next Steps

- **Try the examples:** See [examples/README.md](../examples/README.md) for runnable code
- **Read the docs:** [docs/COPILOT_SKILL.md](./COPILOT_SKILL.md) for full integration guide
- **Explore the CLI:** [docs/cli.md](./cli.md) for command-line workflows
- **Check the roadmap:** [docs/PLAN.md](./PLAN.md) for upcoming features

---

## License

MIT License — see [LICENSE](../LICENSE) for details.
