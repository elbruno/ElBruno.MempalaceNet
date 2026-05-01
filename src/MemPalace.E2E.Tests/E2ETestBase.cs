using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.E2E.Tests;

/// <summary>
/// Base class and shared fixtures for E2E tests.
/// Provides temporary directories, backends, and test data setup.
/// </summary>
public abstract class E2ETestBase : IAsyncDisposable
{
    protected readonly string TempDir;
    protected IBackend Backend = null!;
    protected ICollection Collection = null!;
    protected FakeEmbedder Embedder = null!;
    protected PalaceRef PalaceRef = null!;
    private bool _disposed = false;

    protected E2ETestBase()
    {
        TempDir = Path.Combine(Path.GetTempPath(), $"e2e-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(TempDir);
    }

    protected async Task InitializePalaceAsync(string collectionName = "memories")
    {
        Backend = new SqliteBackend(TempDir);
        Embedder = new FakeEmbedder();
        PalaceRef = new PalaceRef($"test-palace-{Guid.NewGuid()}");
        Collection = await Backend.GetCollectionAsync(
            PalaceRef,
            collectionName,
            create: true,
            embedder: Embedder
        );
    }

    protected async Task<List<EmbeddedRecord>> CreateTestMemoriesAsync(int count, string wing = "test")
    {
        var records = new List<EmbeddedRecord>();
        var contents = Enumerable.Range(0, count)
            .Select(i => $"Memory {i}: This is test content with unique semantic meaning {Guid.NewGuid()}")
            .ToList();

        var embeddings = await Embedder.EmbedAsync(contents);

        for (int i = 0; i < count; i++)
        {
            records.Add(new EmbeddedRecord(
                Id: $"mem-{Guid.NewGuid()}",
                Document: contents[i],
                Metadata: new Dictionary<string, object?>
                {
                    { "wing", wing },
                    { "room", i % 3 == 0 ? "important" : "general" },
                    { "timestamp", DateTimeOffset.UtcNow.AddMinutes(-i).ToUnixTimeSeconds() },
                    { "source", "e2e-test" }
                },
                Embedding: embeddings[i].ToArray()
            ));
        }

        return records;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (Collection != null)
            await Collection.DisposeAsync();
        if (Backend != null)
            await Backend.DisposeAsync();
        if (Directory.Exists(TempDir))
            try { Directory.Delete(TempDir, recursive: true); }
            catch { /* Best effort cleanup */ }
    }
}

/// <summary>
/// Helper for creating test directories with realistic file structures.
/// </summary>
public sealed class TestDirectoryBuilder
{
    private readonly string _baseDir;

    public TestDirectoryBuilder(string baseDir)
    {
        _baseDir = baseDir;
        Directory.CreateDirectory(baseDir);
    }

    public TestDirectoryBuilder AddTextFile(string relPath, string content)
    {
        var fullPath = Path.Combine(_baseDir, relPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
        return this;
    }

    public TestDirectoryBuilder AddMarkdownFile(string relPath, string content)
        => AddTextFile(relPath, content);

    public TestDirectoryBuilder AddJsonFile(string relPath, string jsonContent)
        => AddTextFile(relPath, jsonContent);

    public TestDirectoryBuilder AddBinaryFile(string relPath, byte[] data)
    {
        var fullPath = Path.Combine(_baseDir, relPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, data);
        return this;
    }

    public TestDirectoryBuilder AddDirectory(string relPath)
    {
        var fullPath = Path.Combine(_baseDir, relPath);
        Directory.CreateDirectory(fullPath);
        return this;
    }

    public string BaseDirectory => _baseDir;
}
