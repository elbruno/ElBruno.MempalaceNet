using FluentAssertions;
using MemPalace.Agents.Diary;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Search;
using System.Text;
using Xunit.Abstractions;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for multi-agent memory continuity (agent diary pattern).
/// Validates: memory persistence across turns, context injection, coherence checks.
/// </summary>
public sealed class MultiAgentMemoryTests : E2ETestBase
{
    private readonly ITestOutputHelper _output;

    public MultiAgentMemoryTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public async Task TestAgentMemoryPersistenceAcrossTurns()
    {
        // Arrange: Create agent diary
        await InitializePalaceAsync();
        var diary = new BackedByPalaceDiary(Backend, Embedder, 
            new VectorSearchService(Backend, Embedder));
        
        var agentId = "scribe";

        // Act: Store memories across multiple turns
        var turn1 = new DiaryEntry(
            AgentId: agentId,
            At: DateTimeOffset.UtcNow,
            Role: "assistant",
            Content: "I'm implementing JWT auth validation. Need to handle token expiry and refresh cycles.",
            Metadata: new Dictionary<string, object?> { { "turn", 1 } }
        );

        var turn2 = new DiaryEntry(
            AgentId: agentId,
            At: DateTimeOffset.UtcNow.AddMinutes(5),
            Role: "assistant",
            Content: "JWT validation complete. Now working on role-based access control (RBAC).",
            Metadata: new Dictionary<string, object?> { { "turn", 2 } }
        );

        await diary.AppendAsync(agentId, turn1);
        await diary.AppendAsync(agentId, turn2);

        // Act: Search diary for auth context
        var authContext = await diary.SearchAsync(agentId, "auth context", topK: 5);

        // Assert: Both memories should be retrieved
        authContext.Should().HaveCountGreaterThanOrEqualTo(2, 
            "agent should recall both auth-related memories");

        var hasJwtMemory = authContext.Any(e => e.Content.Contains("JWT"));
        var hasRbacMemory = authContext.Any(e => e.Content.Contains("RBAC"));

        hasJwtMemory.Should().BeTrue("should retrieve JWT auth memory");
        hasRbacMemory.Should().BeTrue("should retrieve RBAC memory");

        // Calculate recall rate (R@5)
        var recallRate = authContext.Count / 2.0; // 2 relevant memories
        recallRate.Should().BeGreaterThanOrEqualTo(0.80, 
            "recall@5 should be ≥80% for agent diary search");

        _output.WriteLine($"Recalled {authContext.Count}/2 memories (R@5: {recallRate * 100:F1}%)");
        _output.WriteLine($"Memory 1: {authContext[0].Content.Substring(0, Math.Min(60, authContext[0].Content.Length))}...");
        _output.WriteLine($"Memory 2: {authContext[1].Content.Substring(0, Math.Min(60, authContext[1].Content.Length))}...");
    }

    [Fact]
    public async Task TestMemoryContextInjection()
    {
        // Arrange: Create agent with memories
        await InitializePalaceAsync();
        var diary = new BackedByPalaceDiary(Backend, Embedder,
            new VectorSearchService(Backend, Embedder));

        var agentId = "assistant";

        var memories = new[]
        {
            "User prefers async/await over Task.Result blocking calls",
            "User's project uses .NET 8 with nullable reference types enabled",
            "User's code style: 4 spaces for indentation, no tabs"
        };

        foreach (var memory in memories)
        {
            await diary.AppendAsync(agentId, new DiaryEntry(
                agentId, DateTimeOffset.UtcNow, "assistant", memory, null));
        }

        // Act: Retrieve and format context for LLM prompt
        var recentMemories = await diary.RecentAsync(agentId, take: 10);
        var contextForPrompt = FormatMemoriesForLLM(recentMemories);

        // Assert: Context should contain all memories in proper format
        contextForPrompt.Should().Contain("async/await", "context should include async preference");
        contextForPrompt.Should().Contain(".NET 8", "context should include framework version");
        contextForPrompt.Should().Contain("4 spaces", "context should include code style");

        // Assert: Format should be LLM-friendly (one memory per line with prefix)
        var lines = contextForPrompt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterThanOrEqualTo(memories.Length,
            "each memory should be on a separate line");

        _output.WriteLine("=== Formatted Context for LLM ===");
        _output.WriteLine(contextForPrompt);
        _output.WriteLine($"Context size: {contextForPrompt.Length} chars");
    }

    [Fact]
    public async Task TestMemoryCoherence()
    {
        // Arrange: Create memories with potential contradictions
        await InitializePalaceAsync();
        var diary = new BackedByPalaceDiary(Backend, Embedder,
            new VectorSearchService(Backend, Embedder));

        var agentId = "reviewer";

        var coherentMemories = new[]
        {
            "User is working on authentication module",
            "Authentication uses JWT tokens with 15-minute expiry",
            "Refresh tokens are stored in HttpOnly cookies"
        };

        foreach (var memory in coherentMemories)
        {
            await diary.AppendAsync(agentId, new DiaryEntry(
                agentId, DateTimeOffset.UtcNow, "assistant", memory, null));
        }

        // Act: Retrieve and check coherence
        var allMemories = await diary.RecentAsync(agentId, take: 10);
        var contextText = string.Join(" ", allMemories.Select(m => m.Content));
        var contradictions = DetectContradictions(contextText);

        // Assert: No contradictions in coherent memories
        contradictions.Should().BeEmpty("coherent memories should not contain contradictions");

        _output.WriteLine($"Coherence check passed: {allMemories.Count} memories, 0 contradictions");
        foreach (var memory in allMemories)
        {
            _output.WriteLine($"  - {memory.Content}");
        }
    }

    [Fact]
    public async Task TestAgentMemorySwitching()
    {
        // Arrange: Create two separate agent diaries
        await InitializePalaceAsync();
        var diary = new BackedByPalaceDiary(Backend, Embedder,
            new VectorSearchService(Backend, Embedder));

        var agent1 = "agent-alpha";
        var agent2 = "agent-beta";

        // Agent 1 memories: authentication focus
        await diary.AppendAsync(agent1, new DiaryEntry(
            agent1, DateTimeOffset.UtcNow, "assistant",
            "Working on OAuth2 authentication flow", null));
        await diary.AppendAsync(agent1, new DiaryEntry(
            agent1, DateTimeOffset.UtcNow, "assistant",
            "Added JWT token validation middleware", null));

        // Agent 2 memories: database focus
        await diary.AppendAsync(agent2, new DiaryEntry(
            agent2, DateTimeOffset.UtcNow, "assistant",
            "Optimizing database query performance", null));
        await diary.AppendAsync(agent2, new DiaryEntry(
            agent2, DateTimeOffset.UtcNow, "assistant",
            "Added connection pooling to reduce latency", null));

        // Act: Search each agent's diary
        var agent1Results = await diary.SearchAsync(agent1, "authentication", topK: 5);
        var agent2Results = await diary.SearchAsync(agent2, "database", topK: 5);

        // Assert: Agent 1 should only retrieve auth memories
        agent1Results.Should().NotBeEmpty("agent 1 should have auth memories");
        agent1Results.Should().OnlyContain(e => 
            e.Content.Contains("OAuth2") || e.Content.Contains("JWT"),
            "agent 1 should only see its own auth-related memories");

        // Assert: Agent 2 should only retrieve database memories
        agent2Results.Should().NotBeEmpty("agent 2 should have database memories");
        agent2Results.Should().OnlyContain(e =>
            e.Content.Contains("database") || e.Content.Contains("pooling"),
            "agent 2 should only see its own database-related memories");

        // Assert: Memory isolation (no cross-contamination)
        var agent1HasDbMemory = agent1Results.Any(e => e.Content.Contains("database"));
        var agent2HasAuthMemory = agent2Results.Any(e => e.Content.Contains("OAuth2"));

        agent1HasDbMemory.Should().BeFalse("agent 1 should not see agent 2's memories");
        agent2HasAuthMemory.Should().BeFalse("agent 2 should not see agent 1's memories");

        _output.WriteLine($"Agent 1 ({agent1}): {agent1Results.Count} auth memories");
        _output.WriteLine($"Agent 2 ({agent2}): {agent2Results.Count} database memories");
        _output.WriteLine("Memory isolation verified ✓");
    }

    // Helper methods

    private static string FormatMemoriesForLLM(IReadOnlyList<DiaryEntry> memories)
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

    private static List<string> DetectContradictions(string contextText)
    {
        var contradictions = new List<string>();
        var text = contextText.ToLowerInvariant();

        // Simple contradiction patterns (extend with more sophisticated NLP if needed)
        var contradictionPairs = new[]
        {
            ("uses jwt", "does not use jwt"),
            ("15-minute expiry", "30-minute expiry"),
            ("oauth2", "basic auth"),
            ("redis cache", "no cache"),
            ("async", "synchronous")
        };

        foreach (var (phrase1, phrase2) in contradictionPairs)
        {
            if (text.Contains(phrase1) && text.Contains(phrase2))
            {
                contradictions.Add($"Contradiction detected: '{phrase1}' vs '{phrase2}'");
            }
        }

        return contradictions;
    }
}
