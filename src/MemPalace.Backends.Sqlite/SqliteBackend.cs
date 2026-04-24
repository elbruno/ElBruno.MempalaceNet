using MemPalace.Core.Backends;
using MemPalace.Core.Errors;
using MemPalace.Core.Model;
using Microsoft.Data.Sqlite;

namespace MemPalace.Backends.Sqlite;

/// <summary>
/// SQLite-based storage backend. Each palace is a separate SQLite database file.
/// </summary>
public sealed class SqliteBackend : IBackend
{
    private readonly string _baseDirectory;
    private readonly Dictionary<string, SqliteConnection> _connections = new();
    private readonly object _lock = new();
    private bool _disposed;

    public SqliteBackend(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemPalace");
    }

    public async ValueTask<ICollection> GetCollectionAsync(
        PalaceRef palace,
        string collectionName,
        bool create = false,
        IEmbedder? embedder = null,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var connection = await GetOrCreateConnectionAsync(palace, create, ct);
        
        var tableName = $"collection_{collectionName}";
        var exists = await TableExistsAsync(connection, tableName, ct);

        if (!exists && !create)
        {
            throw new PalaceNotFoundException($"Collection '{collectionName}' does not exist in palace '{palace.Id}'");
        }

        if (!exists && create)
        {
            if (embedder == null)
            {
                throw new ArgumentException("Embedder is required when creating a new collection");
            }

            await CreateCollectionTablesAsync(connection, collectionName, embedder, ct);
            return new SqliteCollection(connection, collectionName, embedder.Dimensions, embedder.ModelIdentity);
        }

        var (dimensions, embedderIdentity) = await GetCollectionMetadataAsync(connection, collectionName, ct);

        if (embedder != null && embedder.ModelIdentity != embedderIdentity)
        {
            throw new EmbedderIdentityMismatchException(
                $"Collection '{collectionName}' was created with embedder '{embedderIdentity}' but provided embedder is '{embedder.ModelIdentity}'");
        }

        return new SqliteCollection(connection, collectionName, dimensions, embedderIdentity);
    }

    public async ValueTask<IReadOnlyList<string>> ListCollectionsAsync(PalaceRef palace, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var connection = await GetOrCreateConnectionAsync(palace, create: false, ct);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'collection_%'";

        var collections = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var tableName = reader.GetString(0);
            if (tableName.StartsWith("collection_"))
            {
                collections.Add(tableName.Substring("collection_".Length));
            }
        }

        return collections;
    }

    public async ValueTask DeleteCollectionAsync(PalaceRef palace, string name, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        var connection = await GetOrCreateConnectionAsync(palace, create: false, ct);
        
        var tableName = $"collection_{name}";
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS [{tableName}]";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = "DELETE FROM _meta WHERE collection_name = @name";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask<HealthStatus> HealthAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();

        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(ct);
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);

            return HealthStatus.Healthy("SQLite backend operational");
        }
        catch (Exception ex)
        {
            return HealthStatus.Unhealthy($"SQLite backend error: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        lock (_lock)
        {
            foreach (var connection in _connections.Values)
            {
                connection.Dispose();
            }
            _connections.Clear();
            _disposed = true;
        }

        await ValueTask.CompletedTask;
    }

    private async ValueTask<SqliteConnection> GetOrCreateConnectionAsync(PalaceRef palace, bool create, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(palace.Id, out var existingConnection))
            {
                return existingConnection;
            }
        }

        var palacePath = palace.LocalPath ?? Path.Combine(_baseDirectory, palace.Id);
        var dbPath = Path.Combine(palacePath, "palace.db");

        if (!create && !File.Exists(dbPath))
        {
            throw new PalaceNotFoundException($"Palace '{palace.Id}' not found at '{dbPath}'");
        }

        if (create && !Directory.Exists(palacePath))
        {
            Directory.CreateDirectory(palacePath);
        }

        var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync(ct);

        await InitializeDatabaseAsync(connection, ct);

        lock (_lock)
        {
            if (!_connections.ContainsKey(palace.Id))
            {
                _connections[palace.Id] = connection;
            }
        }

        return connection;
    }

    private async Task InitializeDatabaseAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS _meta (
                collection_name TEXT PRIMARY KEY,
                embedder_identity TEXT NOT NULL,
                dimensions INTEGER NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task CreateCollectionTablesAsync(SqliteConnection connection, string collectionName, IEmbedder embedder, CancellationToken ct)
    {
        var tableName = $"collection_{collectionName}";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            CREATE TABLE [{tableName}] (
                id TEXT PRIMARY KEY,
                document TEXT NOT NULL,
                metadata TEXT NOT NULL,
                embedding BLOB NOT NULL,
                dim INTEGER NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = @"
            INSERT INTO _meta (collection_name, embedder_identity, dimensions)
            VALUES (@name, @identity, @dim)";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@name", collectionName);
        cmd.Parameters.AddWithValue("@identity", embedder.ModelIdentity);
        cmd.Parameters.AddWithValue("@dim", embedder.Dimensions);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<(int dimensions, string embedderIdentity)> GetCollectionMetadataAsync(
        SqliteConnection connection, string collectionName, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT dimensions, embedder_identity FROM _meta WHERE collection_name = @name";
        cmd.Parameters.AddWithValue("@name", collectionName);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException($"Collection '{collectionName}' metadata not found");
        }

        return (reader.GetInt32(0), reader.GetString(1));
    }

    private async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new BackendClosedException("Backend has been disposed");
        }
    }
}
