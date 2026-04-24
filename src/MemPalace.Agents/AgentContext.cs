using Microsoft.Extensions.AI;

namespace MemPalace.Agents;

/// <summary>
/// Context passed to an agent during invocation.
/// </summary>
public record AgentContext(
    string ConversationId,
    IReadOnlyList<ChatMessage> History,
    IReadOnlyDictionary<string, object?> Metadata);
