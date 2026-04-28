using MemPalace.Core.Backends;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
using MemPalace.Ai.Summarization;
using NSubstitute;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class KgQueryToolTests
{
    [Fact]
    public async Task KgQuery_ReturnsTriples()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        var expectedTriples = new List<TemporalTriple>
        {
            new(
                new Triple(
                    new EntityRef("agent", "roy"),
                    "worked-on",
                    new EntityRef("project", "MemPalace.Mcp"),
                    null
                ),
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
                null,
                DateTimeOffset.Parse("2026-04-24T10:00:00Z")
            )
        };

        knowledgeGraph.QueryAsync(
            Arg.Any<TriplePattern>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedTriples);

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.KgQuery("agent:roy", "worked-on", "?");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Triples);
        Assert.Equal("agent:roy", result.Triples[0].Subject);
        Assert.Equal("worked-on", result.Triples[0].Predicate);
        Assert.Equal("project:MemPalace.Mcp", result.Triples[0].Object);
    }

    [Fact]
    public async Task KgTimeline_ReturnsEvents()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        var expectedEvents = new List<TimelineEvent>
        {
            new(
                new EntityRef("agent", "roy"),
                "worked-on",
                new EntityRef("project", "MemPalace.Mcp"),
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
                "outgoing"
            )
        };

        knowledgeGraph.TimelineAsync(
            Arg.Any<EntityRef>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.KgTimeline("agent:roy");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Events);
        Assert.Equal("agent:roy", result.Events[0].Entity);
        Assert.Equal("worked-on", result.Events[0].Predicate);
        Assert.Equal("project:MemPalace.Mcp", result.Events[0].Other);
    }

    [Fact]
    public async Task PalaceHealth_ReturnsHealthStatus()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        backend.HealthAsync(Arg.Any<CancellationToken>())
            .Returns(new Core.Model.HealthStatus(true, "All systems operational"));

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceHealth();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Ok);
        Assert.Equal("All systems operational", result.Detail);
    }
}
