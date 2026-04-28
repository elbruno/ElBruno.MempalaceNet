namespace MemPalace.Agents.Registry;

/// <summary>
/// Empty agent registry used when no IChatClient is configured.
/// Returns empty list for List() and throws for Get().
/// </summary>
public sealed class EmptyAgentRegistry : IAgentRegistry
{
    /// <summary>
    /// Returns an empty list of agents.
    /// </summary>
    public IReadOnlyList<AgentDescriptor> List() => Array.Empty<AgentDescriptor>();

    /// <summary>
    /// Throws InvalidOperationException because no chat client is configured.
    /// </summary>
    public IMemPalaceAgent Get(string id) => 
        throw new InvalidOperationException(
            "No chat client configured. Register an IChatClient using AddMemPalaceAi() or AddChatClient() before using agents.");
}
