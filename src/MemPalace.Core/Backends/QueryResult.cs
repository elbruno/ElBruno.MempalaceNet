namespace MemPalace.Core.Backends;

/// <summary>
/// Result from a vector similarity query. Lists are nested: outer list = queries, inner list = results per query.
/// </summary>
public sealed record QueryResult(
    IReadOnlyList<IReadOnlyList<string>> Ids,
    IReadOnlyList<IReadOnlyList<string>> Documents,
    IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Metadatas,
    IReadOnlyList<IReadOnlyList<float>> Distances,
    IReadOnlyList<IReadOnlyList<ReadOnlyMemory<float>>>? Embeddings = null)
{
    /// <summary>
    /// Creates an empty result for the specified number of queries.
    /// </summary>
    public static QueryResult Empty(int numQueries, bool embeddingsRequested)
    {
        var emptyList = new List<string>();
        var emptyMetaList = new List<IReadOnlyDictionary<string, object?>>();
        var emptyDistList = new List<float>();
        var emptyEmbedList = embeddingsRequested ? new List<ReadOnlyMemory<float>>() : null;

        var ids = new List<IReadOnlyList<string>>();
        var docs = new List<IReadOnlyList<string>>();
        var metas = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        var dists = new List<IReadOnlyList<float>>();
        var embeds = embeddingsRequested ? new List<IReadOnlyList<ReadOnlyMemory<float>>>() : null;

        for (int i = 0; i < numQueries; i++)
        {
            ids.Add(emptyList);
            docs.Add(emptyList);
            metas.Add(emptyMetaList);
            dists.Add(emptyDistList);
            embeds?.Add(emptyEmbedList!);
        }

        return new QueryResult(
            ids,
            docs,
            metas,
            dists,
            embeds
        );
    }
}
