namespace MemPalace.Agents;

/// <summary>
/// Declarative agent definition.
/// </summary>
public record AgentDescriptor(
    string Id,
    string Name,
    string Persona,
    string Instructions,
    string? Wing = null);
