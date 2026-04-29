using FluentAssertions;
using MemPalace.Agents;
using MemPalace.Core.Backends;
using MemPalace.Ai;
using MemPalace.Core.Model;
using MemPalace.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Text.Json;

namespace MemPalace.Tests.Integration;

/// <summary>
/// Integration tests for MCP + Agent end-to-end workflows.
/// Tests multi-turn agent context, memory recall, and agent diary persistence.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "v070")]
public class McpAgentIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IBackend _backend;
    private readonly IEmbedder _embedder;
    private readonly string _testPalacePath;

    public McpAgentIntegrationTests()
    {
        _testPalacePath = Path.Combine(Path.GetTempPath(), $"mempalace-mcp-agent-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPalacePath);

        var services = new ServiceCollection();
        _embedder = new DeterministicEmbedder(dimensions: 384);
        services.AddSingleton(_embedder);

        _backend = Substitute.For<IBackend>();
        services.AddSingleton(_backend);

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Test: Agent stores memories via MCP tool, then retrieves them in a later turn.
    /// Validates agent memory persistence across conversation turns.
    /// </summary>
    [Fact]
    public async Task AgentStoresAndRecallsMemories_MultiTurnContext()
    {
        // Arrange
        var agentId = "test-agent-001";
        var palace = new PalaceId(_testPalacePath);
        var diaryCollection = $"agent_diary:{agentId}";

        await using var collection = await _backend.GetCollectionAsync(
            palace,
            diaryCollection,
            create: true,
            _embedder,
            CancellationToken.None);

        // Turn 1: User asks about project status
        var turn1Content = "User asked: What is the status of Project Alpha?";
        var turn1Embedding = await _embedder.EmbedAsync(turn1Content, CancellationToken.None);
        var turn1Record = new MemoryRecord(
            id: Guid.NewGuid().ToString(),
            content: turn1Content,
            embedding: turn1Embedding,
            metadata: new Dictionary<string, object>
            {
                ["agent_id"] = agentId,
                ["turn"] = 1,
                ["role"] = "user"
            }
        );
        await collection.UpsertAsync([turn1Record], CancellationToken.None);

        // Turn 2: Agent responds with status
        var turn2Content = "Agent responded: Project Alpha is 80% complete, targeting Q2 launch";
        var turn2Embedding = await _embedder.EmbedAsync(turn2Content, CancellationToken.None);
        var turn2Record = new MemoryRecord(
            id: Guid.NewGuid().ToString(),
            content: turn2Content,
            embedding: turn2Embedding,
            metadata: new Dictionary<string, object>
            {
                ["agent_id"] = agentId,
                ["turn"] = 2,
                ["role"] = "assistant"
            }
        );
        await collection.UpsertAsync([turn2Record], CancellationToken.None);

        // Turn 3: User follows up (agent needs to recall context)
        var query = "When is the launch?";
        var queryEmbedding = await _embedder.EmbedAsync(query, CancellationToken.None);

        // Act: Agent searches diary for context
        var results = await collection.QueryAsync([queryEmbedding], limit: 3, CancellationToken.None);

        // Assert: Agent should retrieve relevant past turns
        results.Should().NotBeEmpty();
        var retrieved = results.First().Results;
        retrieved.Should().HaveCountGreaterThan(0);

        var contents = retrieved.Select(r => r.Content).ToList();
        contents.Should().Contain(c => c.Contains("Q2 launch") || c.Contains("Project Alpha"));
    }

    /// <summary>
    /// Test: Agent uses MCP tools to query palace and incorporate results into response.
    /// Validates agent tool invocation and context injection workflow.
    /// </summary>
    [Fact]
    public async Task AgentUsesToolToQueryPalace_ContextInjection()
    {
        // Arrange
        var palace = new PalaceId(_testPalacePath);
        await using var collection = await _backend.GetCollectionAsync(
            palace,
            "company-docs",
            create: true,
            _embedder,
            CancellationToken.None);

        // Store company policy document
        var policyContent = "Remote work policy: Employees may work remotely up to 3 days per week";
        var policyEmbedding = await _embedder.EmbedAsync(policyContent, CancellationToken.None);
        var policyRecord = new MemoryRecord(
            id: "policy-remote-work",
            content: policyContent,
            embedding: policyEmbedding,
            metadata: new Dictionary<string, object> { ["type"] = "policy" }
        );
        await collection.UpsertAsync([policyRecord], CancellationToken.None);

        // Simulate agent tool call via MCP
        var searchTool = new PalaceSearchTool(_backend, _embedder);
        var toolArgs = JsonSerializer.Serialize(new
        {
            query = "What is the remote work policy?",
            palace = _testPalacePath,
            wing = "company-docs",
            limit = 3
        });

        // Act
        var toolResult = await searchTool.ExecuteAsync(toolArgs, CancellationToken.None);

        // Assert
        toolResult.Should().NotBeNull();
        toolResult.Should().Contain("Remote work policy");
        toolResult.Should().Contain("3 days per week");
    }

    /// <summary>
    /// Test: Agent diary grows over time, older memories are still retrievable.
    /// Validates long-term memory persistence and retrieval quality.
    /// </summary>
    [Fact]
    public async Task AgentDiaryGrows_OlderMemoriesRetrievable()
    {
        // Arrange
        var agentId = "long-running-agent";
        var palace = new PalaceId(_testPalacePath);
        var diaryCollection = $"agent_diary:{agentId}";

        await using var collection = await _backend.GetCollectionAsync(
            palace,
            diaryCollection,
            create: true,
            _embedder,
            CancellationToken.None);

        // Simulate 10 conversation turns over time
        var conversations = new[]
        {
            "Discussed authentication implementation with JWT",
            "User requested database migration strategy",
            "Planned API versioning approach",
            "Reviewed security best practices",
            "Debugged performance issue in search endpoint",
            "Discussed error handling patterns",
            "User asked about testing strategy",
            "Recommended integration test framework",
            "Planned deployment pipeline",
            "Discussed monitoring and observability"
        };

        var timestamp = DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds();
        foreach (var content in conversations)
        {
            var embedding = await _embedder.EmbedAsync(content, CancellationToken.None);
            var record = new MemoryRecord(
                id: Guid.NewGuid().ToString(),
                content: content,
                embedding: embedding,
                metadata: new Dictionary<string, object>
                {
                    ["agent_id"] = agentId,
                    ["timestamp"] = timestamp
                }
            );
            await collection.UpsertAsync([record], CancellationToken.None);
            timestamp += 86400; // +1 day
        }

        // Act: Query for an older memory (authentication discussion from 10 days ago)
        var query = "How did we implement authentication?";
        var queryEmbedding = await _embedder.EmbedAsync(query, CancellationToken.None);
        var results = await collection.QueryAsync([queryEmbedding], limit: 5, CancellationToken.None);

        // Assert: Should retrieve the authentication discussion despite being old
        results.Should().NotBeEmpty();
        var retrieved = results.First().Results;
        retrieved.Should().HaveCountGreaterThan(0);

        var topResult = retrieved.First().Content;
        topResult.Should().Contain("authentication");
        topResult.Should().Contain("JWT");
    }

    /// <summary>
    /// Test: Multiple agents with separate diaries don't interfere with each other.
    /// Validates agent diary isolation.
    /// </summary>
    [Fact]
    public async Task MultipleAgents_SeparateDiaries_NoInterference()
    {
        // Arrange
        var agents = new[]
        {
            ("agent-alice", "Alice specializes in frontend React development"),
            ("agent-bob", "Bob focuses on backend Python microservices"),
            ("agent-charlie", "Charlie manages infrastructure and DevOps")
        };

        var palace = new PalaceId(_testPalacePath);

        // Store diary entries for each agent
        foreach (var (agentId, expertise) in agents)
        {
            var diaryCollection = $"agent_diary:{agentId}";
            await using var collection = await _backend.GetCollectionAsync(
                palace,
                diaryCollection,
                create: true,
                _embedder,
                CancellationToken.None);

            var embedding = await _embedder.EmbedAsync(expertise, CancellationToken.None);
            var record = new MemoryRecord(
                id: Guid.NewGuid().ToString(),
                content: expertise,
                embedding: embedding,
                metadata: new Dictionary<string, object> { ["agent_id"] = agentId }
            );
            await collection.UpsertAsync([record], CancellationToken.None);
        }

        // Act & Assert: Query each agent's diary and verify isolation
        foreach (var (agentId, expectedExpertise) in agents)
        {
            var diaryCollection = $"agent_diary:{agentId}";
            await using var collection = await _backend.GetCollectionAsync(
                palace,
                diaryCollection,
                create: false,
                _embedder,
                CancellationToken.None);

            var query = "What does this agent specialize in?";
            var queryEmbedding = await _embedder.EmbedAsync(query, CancellationToken.None);
            var results = await collection.QueryAsync([queryEmbedding], limit: 1, CancellationToken.None);

            results.Should().NotBeEmpty();
            var retrieved = results.First().Results.First().Content;
            retrieved.Should().Be(expectedExpertise);

            // Verify no leakage from other agents
            var otherAgents = agents.Where(a => a.Item1 != agentId);
            foreach (var (_, otherExpertise) in otherAgents)
            {
                retrieved.Should().NotContain(otherExpertise);
            }
        }
    }

    /// <summary>
    /// Test: Agent context grows large (100+ memories), search remains performant and relevant.
    /// Validates scalability of agent diary search.
    /// </summary>
    [Fact]
    public async Task AgentDiaryScalability_LargeContext_PerformantSearch()
    {
        // Arrange
        var agentId = "high-volume-agent";
        var palace = new PalaceId(_testPalacePath);
        var diaryCollection = $"agent_diary:{agentId}";

        await using var collection = await _backend.GetCollectionAsync(
            palace,
            diaryCollection,
            create: true,
            _embedder,
            CancellationToken.None);

        // Store 100 diverse memories
        var topics = new[] { "authentication", "database", "API design", "security", "performance" };
        for (int i = 0; i < 100; i++)
        {
            var topic = topics[i % topics.Length];
            var content = $"Discussion about {topic} - iteration {i}, covering various aspects and edge cases";
            var embedding = await _embedder.EmbedAsync(content, CancellationToken.None);
            var record = new MemoryRecord(
                id: Guid.NewGuid().ToString(),
                content: content,
                embedding: embedding,
                metadata: new Dictionary<string, object>
                {
                    ["agent_id"] = agentId,
                    ["iteration"] = i
                }
            );
            await collection.UpsertAsync([record], CancellationToken.None);
        }

        // Act: Query for specific topic
        var query = "What did we discuss about authentication?";
        var queryEmbedding = await _embedder.EmbedAsync(query, CancellationToken.None);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await collection.QueryAsync([queryEmbedding], limit: 10, CancellationToken.None);
        stopwatch.Stop();

        // Assert: Search should be fast and retrieve relevant results
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 second for 100 memories
        results.Should().NotBeEmpty();
        
        var retrieved = results.First().Results;
        retrieved.Should().HaveCount(10);
        retrieved.All(r => r.Content.Contains("authentication")).Should().BeTrue();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();

        if (Directory.Exists(_testPalacePath))
        {
            try
            {
                Directory.Delete(_testPalacePath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
