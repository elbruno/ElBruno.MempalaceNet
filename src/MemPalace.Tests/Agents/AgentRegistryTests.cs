using MemPalace.Agents;
using MemPalace.Agents.Registry;
using MemPalace.Agents.Runtime;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace MemPalace.Tests.Agents;

public sealed class AgentRegistryTests
{
    [Fact]
    public void EmptyRegistry_List_ReturnsEmpty()
    {
        var registry = new EmptyAgentRegistry();
        var agents = registry.List();
        Assert.Empty(agents);
    }

    [Fact]
    public void EmptyRegistry_Get_ThrowsInvalidOperationException()
    {
        var registry = new EmptyAgentRegistry();
        var ex = Assert.Throws<InvalidOperationException>(() => registry.Get("any-id"));
        Assert.Contains("No chat client configured", ex.Message);
    }

    [Fact]
    public void List_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var chatClient = Substitute.For<IChatClient>();
            var builder = Substitute.For<IMemPalaceAgentBuilder>();
            var registry = new YamlAgentRegistry(tempDir, chatClient, builder);

            var agents = registry.List();

            Assert.Empty(agents);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void List_WithTwoYamlFiles_ReturnsBoth()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "agent1.yaml"), @"
id: agent1
name: Agent One
persona: First agent
instructions: Do things
wing: wing1
");
            File.WriteAllText(Path.Combine(tempDir, "agent2.yaml"), @"
id: agent2
name: Agent Two
persona: Second agent
instructions: Do more things
");

            var chatClient = Substitute.For<IChatClient>();
            var builder = Substitute.For<IMemPalaceAgentBuilder>();
            var registry = new YamlAgentRegistry(tempDir, chatClient, builder);

            var agents = registry.List();

            Assert.Equal(2, agents.Count);
            Assert.Contains(agents, a => a.Id == "agent1");
            Assert.Contains(agents, a => a.Id == "agent2");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Get_ValidAgent_ReturnsAgent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test.yaml"), @"
id: test
name: Test
persona: Test agent
instructions: Test instructions
");

            var chatClient = Substitute.For<IChatClient>();
            var builder = Substitute.For<IMemPalaceAgentBuilder>();
            var mockAgent = Substitute.For<IMemPalaceAgent>();
            mockAgent.Descriptor.Returns(new AgentDescriptor("test", "Test", "Test agent", "Test instructions", null));
            
            builder.Build(Arg.Any<AgentDescriptor>(), Arg.Any<IChatClient>())
                .Returns(mockAgent);

            var registry = new YamlAgentRegistry(tempDir, chatClient, builder);
            var agent = registry.Get("test");

            Assert.NotNull(agent);
            Assert.Equal("test", agent.Descriptor.Id);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
