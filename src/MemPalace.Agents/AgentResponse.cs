using Microsoft.Extensions.AI;

namespace MemPalace.Agents;

/// <summary>
/// Response from an agent invocation.
/// </summary>
public record AgentResponse(
    string Content,
    IReadOnlyList<ChatMessage> NewMessages,
    AgentTrace Trace);
