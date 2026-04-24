using Microsoft.Extensions.AI;

namespace MemPalace.Agents.Runtime;

/// <summary>
/// Builder for MemPalace agents backed by Microsoft Agent Framework.
/// </summary>
public interface IMemPalaceAgentBuilder
{
    /// <summary>
    /// Builds an agent from a descriptor and chat client.
    /// </summary>
    IMemPalaceAgent Build(AgentDescriptor descriptor, IChatClient chatClient);
}
