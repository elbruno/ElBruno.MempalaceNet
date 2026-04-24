namespace MemPalace.KnowledgeGraph;

/// <summary>
/// An event in an entity's timeline.
/// </summary>
/// <param name="Entity">The entity this event belongs to</param>
/// <param name="Predicate">The relationship type</param>
/// <param name="Other">The other entity involved</param>
/// <param name="At">When the event occurred</param>
/// <param name="Direction">"outgoing" if Entity is subject, "incoming" if Entity is object</param>
public sealed record TimelineEvent(
    EntityRef Entity,
    string Predicate,
    EntityRef Other,
    DateTimeOffset At,
    string Direction);
