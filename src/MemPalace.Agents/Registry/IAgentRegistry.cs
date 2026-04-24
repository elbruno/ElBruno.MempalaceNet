namespace MemPalace.Agents.Registry;

/// <summary>
/// Agent registry for discovering and retrieving agents.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Lists all available agents.
    /// </summary>
    IReadOnlyList<AgentDescriptor> List();

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    IMemPalaceAgent Get(string id);
}
