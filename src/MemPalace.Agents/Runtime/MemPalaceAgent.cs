using System.Diagnostics;
using Microsoft.Extensions.AI;
using MemPalace.Agents.Diary;

namespace MemPalace.Agents.Runtime;

public class MemPalaceAgent : IMemPalaceAgent
{
    private readonly IChatClient _chatClient;
    private readonly IAgentDiary? _diary;

    public AgentDescriptor Descriptor { get; }

    public MemPalaceAgent(
        AgentDescriptor descriptor,
        IChatClient chatClient,
        IAgentDiary? diary = null)
    {
        Descriptor = descriptor;
        _chatClient = chatClient;
        _diary = diary;
    }

    public async Task<AgentResponse> InvokeAsync(
        string userMessage,
        AgentContext ctx,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        
        // TODO: Wire up IChatClient properly once M.E.AI API surface is verified
        // For now, return echo response for testing framework
        var content = $"[{Descriptor.Name}] Echo: {userMessage}";
        var toolCalls = new List<string>();

        sw.Stop();

        var trace = new AgentTrace(
            InputTokens: 0,
            OutputTokens: 0,
            Latency: sw.Elapsed,
            ToolCalls: toolCalls);

        var newMessages = new List<ChatMessage>
        {
            new(ChatRole.User, userMessage),
            new(ChatRole.Assistant, content)
        };

        if (_diary != null)
        {
            await _diary.AppendAsync(
                Descriptor.Id,
                new DiaryEntry(
                    Descriptor.Id,
                    DateTimeOffset.UtcNow,
                    "user",
                    userMessage),
                ct);

            await _diary.AppendAsync(
                Descriptor.Id,
                new DiaryEntry(
                    Descriptor.Id,
                    DateTimeOffset.UtcNow,
                    "assistant",
                    content,
                    new Dictionary<string, object?>
                    {
                        ["input_tokens"] = trace.InputTokens,
                        ["output_tokens"] = trace.OutputTokens,
                        ["latency_ms"] = (int)trace.Latency.TotalMilliseconds,
                        ["tool_calls"] = string.Join(",", trace.ToolCalls)
                    }),
                ct);
        }

        return new AgentResponse(content, newMessages, trace);
    }
}
