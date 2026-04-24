namespace MemPalace.KnowledgeGraph;

/// <summary>
/// A knowledge graph triple: subject-predicate-object relationship.
/// </summary>
/// <param name="Subject">The subject entity</param>
/// <param name="Predicate">The relationship type</param>
/// <param name="Object">The object entity</param>
/// <param name="Properties">Optional metadata properties</param>
public sealed record Triple(
    EntityRef Subject,
    string Predicate,
    EntityRef Object,
    IReadOnlyDictionary<string, object?>? Properties = null);
