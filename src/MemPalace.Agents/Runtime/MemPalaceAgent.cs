using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using MemPalace.Agents.Diary;

namespace MemPalace.Agents.Runtime;

public class MemPalaceAgent : IMemPalaceAgent
{
    private readonly AIAgent _agent;
    private readonly IAgentDiary? _diary;

    public AgentDescriptor Descriptor { get; }

    public MemPalaceAgent(
        AgentDescriptor descriptor,
        AIAgent agent,
        IAgentDiary? diary = null)
    {
        Descriptor = descriptor;
        _agent = agent;
        _diary = diary;
    }

    public async Task<AgentResponse> InvokeAsync(
        string userMessage,
        AgentContext ctx,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        
        // Invoke the Microsoft Agent Framework agent
        // The docs show: await agent.RunAsync("message"), which must be an extension method or different overload
        // The base RunAsync signature is: RunAsync(AgentSession?, AgentRunOptions?, CancellationToken)
        // There might be an extension method that takes a string directly
        // Let me try calling it with a message-only parameter
        Microsoft.Agents.AI.AgentResponse agentResponse = await _agent.RunAsync(userMessage);
        
        sw.Stop();

        // Extract tool calls from messages (function call messages)
        var toolCalls = new List<string>();
        if (agentResponse.Messages != null)
        {
            foreach (var msg in agentResponse.Messages)
            {
                if (msg.Contents != null)
                {
                    foreach (var content in msg.Contents)
                    {
                        if (content is FunctionCallContent funcCall)
                        {
                            toolCalls.Add(funcCall.Name);
                        }
                    }
                }
            }
        }

        var trace = new AgentTrace(
            InputTokens: (int)(agentResponse.Usage?.InputTokenCount ?? 0),
            OutputTokens: (int)(agentResponse.Usage?.OutputTokenCount ?? 0),
            Latency: sw.Elapsed,
            ToolCalls: toolCalls);

        var newMessages = new List<ChatMessage>
        {
            new(ChatRole.User, userMessage),
            new(ChatRole.Assistant, agentResponse.Text ?? string.Empty)
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
                    agentResponse.Text ?? string.Empty,
                    new Dictionary<string, object?>
                    {
                        ["input_tokens"] = trace.InputTokens,
                        ["output_tokens"] = trace.OutputTokens,
                        ["latency_ms"] = (int)trace.Latency.TotalMilliseconds,
                        ["tool_calls"] = string.Join(",", trace.ToolCalls)
                    }),
                ct);
        }

        return new AgentResponse(agentResponse.Text ?? string.Empty, newMessages, trace);
    }
}
