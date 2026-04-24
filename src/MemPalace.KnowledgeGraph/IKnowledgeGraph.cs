namespace MemPalace.KnowledgeGraph;

/// <summary>
/// Temporal knowledge graph for tracking entity relationships over time.
/// </summary>
public interface IKnowledgeGraph : IAsyncDisposable
{
    /// <summary>
    /// Add a single triple to the graph.
    /// </summary>
    /// <returns>The database ID of the inserted triple</returns>
    Task<long> AddAsync(TemporalTriple triple, CancellationToken ct = default);

    /// <summary>
    /// Add multiple triples in a single transaction.
    /// </summary>
    /// <returns>Number of triples inserted</returns>
    Task<int> AddManyAsync(IEnumerable<TemporalTriple> triples, CancellationToken ct = default);

    /// <summary>
    /// Query triples matching a pattern, optionally as of a specific time.
    /// </summary>
    Task<IReadOnlyList<TemporalTriple>> QueryAsync(
        TriplePattern pattern,
        DateTimeOffset? at = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get timeline of events for an entity (both outgoing and incoming relationships).
    /// </summary>
    Task<IReadOnlyList<TimelineEvent>> TimelineAsync(
        EntityRef entity,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    /// <summary>
    /// End validity of a triple. Only updates if ValidTo is currently null.
    /// </summary>
    /// <returns>True if updated, false if already ended</returns>
    Task<bool> EndValidityAsync(long tripleId, DateTimeOffset endedAt, CancellationToken ct = default);

    /// <summary>
    /// Count total triples in the graph.
    /// </summary>
    Task<int> CountAsync(CancellationToken ct = default);
}
