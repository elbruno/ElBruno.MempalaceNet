namespace MemPalace.Agents;

/// <summary>
/// MemPalace agent abstraction.
/// </summary>
public interface IMemPalaceAgent
{
    /// <summary>
    /// Gets the agent descriptor.
    /// </summary>
    AgentDescriptor Descriptor { get; }

    /// <summary>
    /// Invokes the agent with a user message.
    /// </summary>
    Task<AgentResponse> InvokeAsync(
        string userMessage,
        AgentContext ctx,
        CancellationToken ct = default);
}
