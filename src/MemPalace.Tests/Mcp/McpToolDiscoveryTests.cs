using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp;
using MemPalace.Search;
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
        var tools = new MemPalaceMcpTools(searchService, backend, knowledgeGraph);

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
