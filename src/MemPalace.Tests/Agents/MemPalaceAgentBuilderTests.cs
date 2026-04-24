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
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Test response"))
        {
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20 }
        };
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(chatResponse);
        
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
        Assert.NotEmpty(response.Content);
        
        await diary.Received(2).AppendAsync(
            "test",
            Arg.Any<DiaryEntry>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ReturnsTraceWithLatency()
    {
        var chatClient = Substitute.For<IChatClient>();
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Test response"))
        {
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20 }
        };
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(chatResponse);
        
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
        Assert.Equal(10, response.Trace.InputTokens);
        Assert.Equal(20, response.Trace.OutputTokens);
    }
}
