namespace MemPalace.Search;

/// <summary>
/// Interface for search services.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for documents matching the query.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        string collection,
        SearchOptions opts,
        CancellationToken ct = default);
}
