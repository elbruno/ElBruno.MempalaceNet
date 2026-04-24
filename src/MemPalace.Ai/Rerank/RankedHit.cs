namespace MemPalace.Ai.Rerank;

/// <summary>
/// Represents a search hit with score and document content.
/// </summary>
public sealed record RankedHit(
    string Id,
    string Document,
    float Score);
