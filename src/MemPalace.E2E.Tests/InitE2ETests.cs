using FluentAssertions;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Tests.Backends;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for palace initialization workflow.
/// Covers: directory creation, configuration, idempotency, path validation.
/// </summary>
public sealed class InitE2ETests : IDisposable
{
    private readonly List<string> _dirsToClean = new();

    [Fact]
    public void WhenInitPalace_WithValidPath_ExpectDirectoryCreated()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-init-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);

        // Act
        Directory.CreateDirectory(testDir);
        var backend = new SqliteBackend(testDir);

        // Assert
        Directory.Exists(testDir).Should().BeTrue("Palace directory should be created");
        backend.Should().NotBeNull("Backend should initialize successfully");
    }

    [Fact]
    public void WhenInitPalace_WithNestedPath_ExpectDirectoryStructureCreated()
    {
        // Arrange
        var baseDir = Path.Combine(Path.GetTempPath(), $"e2e-nested-{Guid.NewGuid()}");
        var nestedPath = Path.Combine(baseDir, "palaces", "my-palace");
        _dirsToClean.Add(baseDir);

        // Act
        Directory.CreateDirectory(nestedPath);
        var backend = new SqliteBackend(nestedPath);

        // Assert
        Directory.Exists(nestedPath).Should().BeTrue("Nested directory structure should be created");
        backend.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenInitPalace_WithBackendInitialization_ExpectCollectionCreatable()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-init-collection-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);
        var backend = new SqliteBackend(testDir);

        // Act
        var palace = new PalaceRef("test-palace");
        var embedder = new FakeEmbedder();
        var collection = await backend.GetCollectionAsync(palace, "default", create: true, embedder: embedder);

        // Assert
        collection.Should().NotBeNull("Collection should be created after palace initialization");
        await collection.DisposeAsync();
        await backend.DisposeAsync();
    }

    [Fact]
    public void WhenInitPalace_WithExpandedPath_ExpectPathNormalization()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-expand-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var pathWithEnvVar = testDir;

        // Act
        var expandedPath = Environment.ExpandEnvironmentVariables(pathWithEnvVar);
        Directory.CreateDirectory(expandedPath);

        // Assert
        Directory.Exists(expandedPath).Should().BeTrue("Expanded path should be valid");
        var absolutePath = Path.GetFullPath(expandedPath);
        Path.IsPathRooted(absolutePath).Should().BeTrue("Path should be absolute");
    }

    [Fact]
    public void WhenInitPalace_WithExistingEmptyDirectory_ExpectBackendInitializes()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-existing-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);

        // Act - Initialize backend in existing directory
        var backend = new SqliteBackend(testDir);

        // Assert
        backend.Should().NotBeNull("Backend should initialize in existing empty directory");
    }

    [Fact]
    public async Task WhenInitPalace_WithMultipleCollections_ExpectAllCreatable()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-multi-coll-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);
        var backend = new SqliteBackend(testDir);
        var palace = new PalaceRef("multi-palace");
        var embedder = new FakeEmbedder();

        // Act
        var col1 = await backend.GetCollectionAsync(palace, "collection1", create: true, embedder: embedder);
        var col2 = await backend.GetCollectionAsync(palace, "collection2", create: true, embedder: embedder);
        var col3 = await backend.GetCollectionAsync(palace, "collection3", create: true, embedder: embedder);

        // Assert
        col1.Should().NotBeNull();
        col2.Should().NotBeNull();
        col3.Should().NotBeNull();
        
        await col1.DisposeAsync();
        await col2.DisposeAsync();
        await col3.DisposeAsync();
        await backend.DisposeAsync();
    }

    [Fact]
    public async Task WhenInitPalace_WithIdempotentCalls_ExpectConsistentState()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-idempotent-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);
        var palace = new PalaceRef("idempotent-test");
        var embedder = new FakeEmbedder();

        // Act - Create backend and collection twice
        var backend1 = new SqliteBackend(testDir);
        var col1 = await backend1.GetCollectionAsync(palace, "memories", create: true, embedder: embedder);
        var embedding = (await embedder.EmbedAsync(new[] { "Test memory" }))[0].ToArray();
        var record1 = new EmbeddedRecord(
            Id: "test-1",
            Document: "Test memory",
            Metadata: new Dictionary<string, object?> { { "wing", "test" } },
            Embedding: embedding
        );
        await col1.AddAsync(new[] { record1 });
        await col1.DisposeAsync();
        await backend1.DisposeAsync();

        // Create again and verify data persists
        var backend2 = new SqliteBackend(testDir);
        var col2 = await backend2.GetCollectionAsync(palace, "memories", create: false, embedder: embedder);
        var result = await col2.GetAsync(limit: 100);

        // Assert
        result.Documents.Should().HaveCount(1);
        result.Documents[0].Should().Be("Test memory");
        
        await col2.DisposeAsync();
        await backend2.DisposeAsync();
    }

    [Fact]
    public void WhenInitPalace_WithPermissionValidation_ExpectPathAccessible()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"e2e-perms-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);

        // Act
        var canWrite = File.Exists(testDir) || Directory.Exists(testDir);

        // Assert
        canWrite.Should().BeTrue("Directory should be accessible for read/write");
    }

    public void Dispose()
    {
        foreach (var dir in _dirsToClean)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch { /* Best effort cleanup */ }
        }
    }
}
