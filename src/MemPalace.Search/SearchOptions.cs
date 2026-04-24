using MemPalace.Core.Backends;

namespace MemPalace.Search;

/// <summary>
/// Options for search operations.
/// </summary>
public sealed record SearchOptions(
    int TopK = 10,
    string? Wing = null,
    WhereClause? Where = null,
    bool Rerank = false,
    float? MinScore = null);
