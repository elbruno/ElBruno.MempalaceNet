using MemPalace.Core.Backends;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
using MemPalace.Ai.Summarization;
using NSubstitute;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class PalaceSearchToolTests
{
    [Fact]
    public async Task PalaceSearch_CallsSearchService()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                new("id1", "doc1", 0.95f, new Dictionary<string, object?> { ["key"] = "value" })
            });

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceSearch("test query", "default", 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Hits);
        Assert.Equal("id1", result.Hits[0].Id);
        Assert.Equal("doc1", result.Hits[0].Document);
        Assert.Equal(0.95f, result.Hits[0].Score);
    }

    [Fact]
    public async Task PalaceRecall_CallsPalaceSearch()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        searchService.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SearchOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                new("id1", "doc1", 0.95f, null)
            });

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceRecall("test query", "default", 5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Hits);
        await searchService.Received(1).SearchAsync(
            "test query",
            "default",
            Arg.Is<SearchOptions>(o => o.TopK == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PalaceListWings_ReturnsCollections()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();
        
        backend.ListCollectionsAsync(
            Arg.Any<Core.Model.PalaceRef>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "wing1", "wing2", "wing3" });

        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Act
        var result = await tools.PalaceListWings("default");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Wings.Length);
        Assert.Contains("wing1", result.Wings);
        Assert.Contains("wing2", result.Wings);
    }
}
