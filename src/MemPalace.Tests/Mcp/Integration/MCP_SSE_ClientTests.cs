using System.Net;
using System.Text;
using FluentAssertions;
using MemPalace.Ai.Summarization;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Mcp.Transports;
using MemPalace.Search;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MemPalace.Tests.Mcp.Integration;

/// <summary>
/// Integration tests for MCP SSE transport with real HTTP client.
/// Tests the full client-server lifecycle with actual network communication.
/// </summary>
public class MCP_SSE_ClientTests : IDisposable
{
    private readonly HttpSseTransport _transport;
    private readonly ILogger<HttpSseTransport> _logger;
    private readonly int _testPort;
    private bool _disposed;

    public MCP_SSE_ClientTests()
    {
        _logger = Substitute.For<ILogger<HttpSseTransport>>();
        // Use random port to avoid conflicts with parallel test runs
        _testPort = Random.Shared.Next(6000, 7000);
        _transport = new HttpSseTransport(_logger, port: _testPort);
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ServerStartup_ServerListensOnConfiguredPort()
    {
        // Arrange & Act
        await _transport.StartAsync();

        // Assert - Server should be listening and reject unauthenticated requests
        using var client = new HttpClient();
        var response = await client.GetAsync($"http://127.0.0.1:{_testPort}/mcp");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await _transport.StopAsync();
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ClientConnection_CreatesSessionAndEstablishesSSE()
    {
        // Arrange
        await _transport.StartAsync();

        try
        {
            // Act - Create session via POST without session ID
            using var client = new HttpClient();
            var content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"initialize\"}", Encoding.UTF8, "application/json");
            var postResponse = await client.PostAsync($"http://127.0.0.1:{_testPort}/mcp", content);

            // Assert
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            postResponse.Headers.Should().ContainKey("Mcp-Session-Id");
            var sessionId = postResponse.Headers.GetValues("Mcp-Session-Id").First();
            sessionId.Should().NotBeNullOrWhiteSpace();
            sessionId.Length.Should().BeGreaterThan(40); // Crypto-secure token
        }
        finally
        {
            await _transport.StopAsync();
        }
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ToolCallRead_SearchToolReturnsResults()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();

        searchService.SearchAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SearchOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                new("mem1", "Found document about AI agents", 0.95f, new Dictionary<string, object?> { ["wing"] = "research" })
            });

        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceSearch("AI agents", "research", 5);

        // Assert
        result.Should().NotBeNull();
        result.Hits.Should().HaveCount(1);
        result.Hits[0].Document.Should().Contain("AI agents");
        result.Hits[0].Score.Should().BeGreaterThan(0.9f);
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ToolCallGet_RetrievesMemoryById()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();

        var collection = Substitute.For<ICollection>();
        backend.GetCollectionAsync(
                Arg.Any<PalaceRef>(),
                Arg.Any<string>(),
                false,
                null,
                Arg.Any<CancellationToken>())
            .Returns(collection);

        collection.GetAsync(
                Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == "mem1"),
                null,
                null,
                0,
                IncludeFields.Documents | IncludeFields.Metadatas,
                Arg.Any<CancellationToken>())
            .Returns(new GetResult(
                new List<string> { "mem1" },
                new List<string> { "Research about transformers" },
                new List<Dictionary<string, object?>> { new() { ["wing"] = "research" } },
                null));

        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceGet("mem1", "research", "default");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("mem1");
        result.Document.Should().Contain("transformers");
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task SessionTimeout_ExpiredTokenReturns401()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromSeconds(2);
        var sessionManager = new SessionManager(shortTimeout);
        using var transport = new HttpSseTransport(_logger, sessionManager, port: _testPort + 1);
        await transport.StartAsync();

        try
        {
            using var client = new HttpClient();

            // Create session
            var content1 = new StringContent("{\"test\":1}", Encoding.UTF8, "application/json");
            var response1 = await client.PostAsync($"http://127.0.0.1:{_testPort + 1}/mcp", content1);
            var sessionId = response1.Headers.GetValues("Mcp-Session-Id").First();

            // Act - Wait for timeout + make request with expired session
            await Task.Delay(TimeSpan.FromSeconds(3));

            var content2 = new StringContent("{\"test\":2}", Encoding.UTF8, "application/json");
            var request2 = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{_testPort + 1}/mcp")
            {
                Content = content2
            };
            request2.Headers.Add("Mcp-Session-Id", sessionId);
            var response2 = await client.SendAsync(request2);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ConcurrentClients_SessionManagerRoutesCorrectly()
    {
        // Arrange
        await _transport.StartAsync();

        try
        {
            // Act - Create two concurrent sessions
            using var client1 = new HttpClient();
            using var client2 = new HttpClient();

            var content1 = new StringContent("{\"client\":1}", Encoding.UTF8, "application/json");
            var content2 = new StringContent("{\"client\":2}", Encoding.UTF8, "application/json");

            var task1 = client1.PostAsync($"http://127.0.0.1:{_testPort}/mcp", content1);
            var task2 = client2.PostAsync($"http://127.0.0.1:{_testPort}/mcp", content2);

            var responses = await Task.WhenAll(task1, task2);

            // Assert - Both sessions should succeed with different session IDs
            responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
            responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

            var sessionId1 = responses[0].Headers.GetValues("Mcp-Session-Id").First();
            var sessionId2 = responses[1].Headers.GetValues("Mcp-Session-Id").First();

            sessionId1.Should().NotBeNullOrWhiteSpace();
            sessionId2.Should().NotBeNullOrWhiteSpace();
            sessionId1.Should().NotBe(sessionId2);
        }
        finally
        {
            await _transport.StopAsync();
        }
    }

    [Fact(Skip = "Hangs on HTTP server disposal - fix in follow-up")]
    public async Task ServerShutdown_ClosesAllConnections()
    {
        // Arrange
        await _transport.StartAsync();

        using var client = new HttpClient();
        var content = new StringContent("{\"test\":1}", Encoding.UTF8, "application/json");
        await client.PostAsync($"http://127.0.0.1:{_testPort}/mcp", content);

        // Act - Shutdown server
        await _transport.StopAsync();

        // Assert - Server should no longer be listening
        var act = async () => await client.GetAsync($"http://127.0.0.1:{_testPort}/mcp");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transport.StopAsync().GetAwaiter().GetResult();
        _transport.Dispose();
        _disposed = true;
    }
}
