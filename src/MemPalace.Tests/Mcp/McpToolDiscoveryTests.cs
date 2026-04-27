using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
using MemPalace.Ai.Summarization;
using NSubstitute;
using Xunit;

namespace MemPalace.Tests.Mcp;

public class McpToolDiscoveryTests
{
    [Fact]
    public void ToolsClass_CanBeInstantiated()
    {
        // Arrange
        var searchService = Substitute.For<ISearchService>();
        var backend = Substitute.For<IBackend>();
        var knowledgeGraph = Substitute.For<IKnowledgeGraph>();

        // Act
        var memorySummarizer = Substitute.For<IMemorySummarizer>();
        var embedder = Substitute.For<IEmbedder>();
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph, memorySummarizer, embedder);

        // Assert
        Assert.NotNull(tools);
    }

    [Theory]
    [InlineData("PalaceSearch")]
    [InlineData("PalaceRecall")]
    [InlineData("PalaceGet")]
    [InlineData("PalaceListWings")]
    [InlineData("KgQuery")]
    [InlineData("KgTimeline")]
    [InlineData("PalaceHealth")]
    public void ToolMethod_Exists(string methodName)
    {
        // Arrange & Act
        var toolsType = typeof(MemPalaceMcpTools);
        var method = toolsType.GetMethod(methodName);

        // Assert
        Assert.NotNull(method);
    }
}
