using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MemPalace.KnowledgeGraph;

/// <summary>
/// SQLite-backed implementation of temporal knowledge graph.
/// </summary>
public sealed class SqliteKnowledgeGraph : IKnowledgeGraph
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    public SqliteKnowledgeGraph(string dbPath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS triples (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                s_type TEXT NOT NULL,
                s_id TEXT NOT NULL,
                predicate TEXT NOT NULL,
                o_type TEXT NOT NULL,
                o_id TEXT NOT NULL,
                props TEXT,
                valid_from TEXT NOT NULL,
                valid_to TEXT,
                recorded_at TEXT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_triples_subject ON triples(s_type, s_id);
            CREATE INDEX IF NOT EXISTS idx_triples_object ON triples(o_type, o_id);
            CREATE INDEX IF NOT EXISTS idx_triples_predicate ON triples(predicate);
            CREATE INDEX IF NOT EXISTS idx_triples_valid ON triples(valid_from, valid_to);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<long> AddAsync(TemporalTriple triple, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        await _writeLock.WaitAsync(ct);
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO triples (s_type, s_id, predicate, o_type, o_id, props, valid_from, valid_to, recorded_at)
                VALUES (@s_type, @s_id, @predicate, @o_type, @o_id, @props, @valid_from, @valid_to, @recorded_at);
                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@s_type", triple.Triple.Subject.Type);
            cmd.Parameters.AddWithValue("@s_id", triple.Triple.Subject.Id);
            cmd.Parameters.AddWithValue("@predicate", triple.Triple.Predicate);
            cmd.Parameters.AddWithValue("@o_type", triple.Triple.Object.Type);
            cmd.Parameters.AddWithValue("@o_id", triple.Triple.Object.Id);
            cmd.Parameters.AddWithValue("@props", triple.Triple.Properties != null
                ? JsonSerializer.Serialize(triple.Triple.Properties)
                : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@valid_from", triple.ValidFrom.UtcDateTime.ToString("O"));
            cmd.Parameters.AddWithValue("@valid_to", triple.ValidTo.HasValue
                ? triple.ValidTo.Value.UtcDateTime.ToString("O")
                : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@recorded_at", triple.RecordedAt.UtcDateTime.ToString("O"));

            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<int> AddManyAsync(IEnumerable<TemporalTriple> triples, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        await _writeLock.WaitAsync(ct);
        try
        {
            using var transaction = _connection.BeginTransaction();
            
            var count = 0;
            foreach (var triple in triples)
            {
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO triples (s_type, s_id, predicate, o_type, o_id, props, valid_from, valid_to, recorded_at)
                    VALUES (@s_type, @s_id, @predicate, @o_type, @o_id, @props, @valid_from, @valid_to, @recorded_at);
                ";

                cmd.Parameters.AddWithValue("@s_type", triple.Triple.Subject.Type);
                cmd.Parameters.AddWithValue("@s_id", triple.Triple.Subject.Id);
                cmd.Parameters.AddWithValue("@predicate", triple.Triple.Predicate);
                cmd.Parameters.AddWithValue("@o_type", triple.Triple.Object.Type);
                cmd.Parameters.AddWithValue("@o_id", triple.Triple.Object.Id);
                cmd.Parameters.AddWithValue("@props", triple.Triple.Properties != null
                    ? JsonSerializer.Serialize(triple.Triple.Properties)
                    : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@valid_from", triple.ValidFrom.UtcDateTime.ToString("O"));
                cmd.Parameters.AddWithValue("@valid_to", triple.ValidTo.HasValue
                    ? triple.ValidTo.Value.UtcDateTime.ToString("O")
                    : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@recorded_at", triple.RecordedAt.UtcDateTime.ToString("O"));

                await cmd.ExecuteNonQueryAsync(ct);
                count++;
            }

            await transaction.CommitAsync(ct);
            return count;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<TemporalTriple>> QueryAsync(
        TriplePattern pattern,
        DateTimeOffset? at = null,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        using var cmd = _connection.CreateCommand();
        var whereClauses = new List<string>();
        
        if (pattern.Subject != null)
        {
            whereClauses.Add("s_type = @s_type AND s_id = @s_id");
            cmd.Parameters.AddWithValue("@s_type", pattern.Subject.Type);
            cmd.Parameters.AddWithValue("@s_id", pattern.Subject.Id);
        }

        if (pattern.Predicate != null)
        {
            whereClauses.Add("predicate = @predicate");
            cmd.Parameters.AddWithValue("@predicate", pattern.Predicate);
        }

        if (pattern.Object != null)
        {
            whereClauses.Add("o_type = @o_type AND o_id = @o_id");
            cmd.Parameters.AddWithValue("@o_type", pattern.Object.Type);
            cmd.Parameters.AddWithValue("@o_id", pattern.Object.Id);
        }

        if (at.HasValue)
        {
            whereClauses.Add("valid_from <= @at AND (valid_to IS NULL OR @at < valid_to)");
            cmd.Parameters.AddWithValue("@at", at.Value.UtcDateTime.ToString("O"));
        }

        cmd.CommandText = $@"
            SELECT id, s_type, s_id, predicate, o_type, o_id, props, valid_from, valid_to, recorded_at
            FROM triples
            {(whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "")}
            ORDER BY valid_from ASC
        ";

        var results = new List<TemporalTriple>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            var subject = new EntityRef(reader.GetString(1), reader.GetString(2));
            var predicate = reader.GetString(3);
            var obj = new EntityRef(reader.GetString(4), reader.GetString(5));
            
            IReadOnlyDictionary<string, object?>? props = null;
            if (!reader.IsDBNull(6))
            {
                var propsJson = reader.GetString(6);
                props = JsonSerializer.Deserialize<Dictionary<string, object?>>(propsJson);
            }

            var validFrom = DateTimeOffset.Parse(reader.GetString(7));
            DateTimeOffset? validTo = reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8));
            var recordedAt = DateTimeOffset.Parse(reader.GetString(9));

            var triple = new Triple(subject, predicate, obj, props);
            results.Add(new TemporalTriple(triple, validFrom, validTo, recordedAt));
        }

        return results;
    }

    public async Task<IReadOnlyList<TimelineEvent>> TimelineAsync(
        EntityRef entity,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        using var cmd = _connection.CreateCommand();
        
        var whereClauses = new List<string>
        {
            "((s_type = @type AND s_id = @id) OR (o_type = @type AND o_id = @id))"
        };

        cmd.Parameters.AddWithValue("@type", entity.Type);
        cmd.Parameters.AddWithValue("@id", entity.Id);

        if (from.HasValue)
        {
            whereClauses.Add("valid_from >= @from");
            cmd.Parameters.AddWithValue("@from", from.Value.UtcDateTime.ToString("O"));
        }

        if (to.HasValue)
        {
            whereClauses.Add("valid_from < @to");
            cmd.Parameters.AddWithValue("@to", to.Value.UtcDateTime.ToString("O"));
        }

        cmd.CommandText = $@"
            SELECT s_type, s_id, predicate, o_type, o_id, valid_from
            FROM triples
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY valid_from ASC
        ";

        var results = new List<TimelineEvent>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            var subjectType = reader.GetString(0);
            var subjectId = reader.GetString(1);
            var predicate = reader.GetString(2);
            var objectType = reader.GetString(3);
            var objectId = reader.GetString(4);
            var validFrom = DateTimeOffset.Parse(reader.GetString(5));

            var isSubject = subjectType == entity.Type && subjectId == entity.Id;
            var direction = isSubject ? "outgoing" : "incoming";
            var other = isSubject
                ? new EntityRef(objectType, objectId)
                : new EntityRef(subjectType, subjectId);

            results.Add(new TimelineEvent(entity, predicate, other, validFrom, direction));
        }

        return results;
    }

    public async Task<bool> EndValidityAsync(long tripleId, DateTimeOffset endedAt, CancellationToken ct = default)
    {
        EnsureNotDisposed();

        await _writeLock.WaitAsync(ct);
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE triples
                SET valid_to = @valid_to
                WHERE id = @id AND valid_to IS NULL
            ";

            cmd.Parameters.AddWithValue("@id", tripleId);
            cmd.Parameters.AddWithValue("@valid_to", endedAt.UtcDateTime.ToString("O"));

            var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
            return rowsAffected > 0;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM triples";
        
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _connection.DisposeAsync();
        _writeLock.Dispose();
        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SqliteKnowledgeGraph));
        }
    }
}
