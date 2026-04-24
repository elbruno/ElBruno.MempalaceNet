namespace MemPalace.Core.Backends;

/// <summary>
/// Flags for specifying which fields to include in query/get results.
/// </summary>
[Flags]
public enum IncludeFields
{
    Documents = 1,
    Metadatas = 2,
    Distances = 4,
    Embeddings = 8
}
