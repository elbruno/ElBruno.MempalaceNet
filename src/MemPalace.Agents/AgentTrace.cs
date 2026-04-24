namespace MemPalace.Agents;

/// <summary>
/// Trace information from an agent invocation.
/// </summary>
public record AgentTrace(
    int InputTokens,
    int OutputTokens,
    TimeSpan Latency,
    IReadOnlyList<string> ToolCalls);
