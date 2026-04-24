using MemPalace.Core.Model;

namespace MemPalace.Core.Backends;

/// <summary>
/// Represents a collection (table/index) within a palace that stores embeddings.
/// </summary>
public interface ICollection : IAsyncDisposable
{
    /// <summary>
    /// The name of this collection.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The expected dimensionality of embeddings in this collection.
    /// </summary>
    int Dimensions { get; }

    /// <summary>
    /// The embedder identity string that created this collection.
    /// </summary>
    string EmbedderIdentity { get; }

    /// <summary>
    /// Adds new records. Throws if any ID already exists.
    /// </summary>
    ValueTask AddAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default);

    /// <summary>
    /// Upserts records (insert or update).
    /// </summary>
    ValueTask UpsertAsync(
        IReadOnlyList<EmbeddedRecord> records,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves records by IDs or filter.
    /// </summary>
    ValueTask<GetResult> GetAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        int? limit = null,
        int offset = 0,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas,
        CancellationToken ct = default);

    /// <summary>
    /// Queries for nearest neighbors using vector similarity.
    /// </summary>
    ValueTask<QueryResult> QueryAsync(
        IReadOnlyList<ReadOnlyMemory<float>> queryEmbeddings,
        int nResults = 10,
        WhereClause? where = null,
        IncludeFields include = IncludeFields.Documents | IncludeFields.Metadatas | IncludeFields.Distances,
        CancellationToken ct = default);

    /// <summary>
    /// Counts records in the collection.
    /// </summary>
    ValueTask<long> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes records by IDs or filter.
    /// </summary>
    ValueTask DeleteAsync(
        IReadOnlyList<string>? ids = null,
        WhereClause? where = null,
        CancellationToken ct = default);
}
