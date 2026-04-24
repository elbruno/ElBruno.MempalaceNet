namespace MemPalace.KnowledgeGraph;

/// <summary>
/// Reference to an entity in the knowledge graph.
/// </summary>
/// <param name="Type">Entity type (e.g., "agent", "project", "session")</param>
/// <param name="Id">Entity identifier (e.g., "tyrell", "MemPalace.Core", "abc-123")</param>
public sealed record EntityRef(string Type, string Id)
{
    public override string ToString() => $"{Type}:{Id}";

    public static EntityRef Parse(string value)
    {
        var parts = value.Split(':', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid EntityRef format: '{value}'. Expected 'type:id'.", nameof(value));
        }
        return new EntityRef(parts[0], parts[1]);
    }
}
