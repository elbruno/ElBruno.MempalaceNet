namespace MemPalace.KnowledgeGraph;

/// <summary>
/// A pattern for matching triples. Null fields act as wildcards.
/// </summary>
/// <param name="Subject">Match specific subject, or null for wildcard</param>
/// <param name="Predicate">Match specific predicate, or null for wildcard</param>
/// <param name="Object">Match specific object, or null for wildcard</param>
public sealed record TriplePattern(
    EntityRef? Subject,
    string? Predicate,
    EntityRef? Object);
