using MemPalace.Agents;
using MemPalace.Agents.Diary;
using MemPalace.Agents.Runtime;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace MemPalace.Tests.Agents;

public sealed class MemPalaceAgentBuilderTests
{
    [Fact]
    public void Build_CreatesAgentWithDescriptor()
    {
        var chatClient = Substitute.For<IChatClient>();
        var diary = Substitute.For<IAgentDiary>();
        var builder = new MemPalaceAgentBuilder(diary: diary);

        var descriptor = new AgentDescriptor("test", "Test", "You are a test", "Do testing", null);
        var agent = builder.Build(descriptor, chatClient);

        Assert.NotNull(agent);
        Assert.Equal("test", agent.Descriptor.Id);
        Assert.Equal("Test", agent.Descriptor.Name);
    }

    [Fact]
    public async Task InvokeAsync_RecordsTurnToDiary()
    {
        var chatClient = Substitute.For<IChatClient>();
        var diary = Substitute.For<IAgentDiary>();
        var builder = new MemPalaceAgentBuilder(diary: diary);

        var descriptor = new AgentDescriptor("test", "Test", "You are a test", "Do testing", null);
        var agent = builder.Build(descriptor, chatClient);

        var ctx = new AgentContext(
            "conversation-1",
            Array.Empty<ChatMessage>(),
            new Dictionary<string, object?>());

        var response = await agent.InvokeAsync("Hello", ctx);

        Assert.NotNull(response);
        Assert.Contains("Echo", response.Content);
        
        await diary.Received(2).AppendAsync(
            "test",
            Arg.Any<DiaryEntry>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ReturnsTraceWithLatency()
    {
        var chatClient = Substitute.For<IChatClient>();
        var builder = new MemPalaceAgentBuilder();

        var descriptor = new AgentDescriptor("test", "Test", "You are a test", "Do testing", null);
        var agent = builder.Build(descriptor, chatClient);

        var ctx = new AgentContext(
            "conversation-1",
            Array.Empty<ChatMessage>(),
            new Dictionary<string, object?>());

        var response = await agent.InvokeAsync("Hello", ctx);

        Assert.NotNull(response.Trace);
        Assert.True(response.Trace.Latency.TotalMilliseconds >= 0);
        Assert.NotNull(response.Trace.ToolCalls);
    }
}
