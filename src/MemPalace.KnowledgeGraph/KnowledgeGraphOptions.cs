namespace MemPalace.KnowledgeGraph;

/// <summary>
/// Configuration options for the knowledge graph.
/// </summary>
public sealed class KnowledgeGraphOptions
{
    /// <summary>
    /// Path to the SQLite database file. Defaults to "{PalaceDirectory}/mempalace-kg.db"
    /// </summary>
    public string DatabasePath { get; set; } = string.Empty;
}
