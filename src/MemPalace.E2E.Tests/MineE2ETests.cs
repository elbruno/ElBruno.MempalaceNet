using FluentAssertions;
using MemPalace.Mining;

namespace MemPalace.E2E.Tests;

/// <summary>
/// E2E tests for file mining workflow.
/// Covers: file discovery, content extraction, metadata, encoding, error handling.
/// </summary>
public sealed class MineE2ETests : IDisposable
{
    private readonly List<string> _dirsToClean = new();

    [Fact]
    public async Task WhenMineSingleTextFile_ExpectContentExtracted()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-single-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("doc.txt", "This is test content for mining");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Content.Should().Contain("test content");
        items[0].Metadata.Should().ContainKey("path");
        items[0].Metadata.Should().ContainKey("ext");
        items[0].Metadata.Should().ContainKey("size");
    }

    [Fact]
    public async Task WhenMineMultipleFiles_ExpectAllDiscovered()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-multi-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("file1.txt", "Content 1")
                .AddTextFile("file2.md", "# Content 2")
                .AddTextFile("file3.txt", "Content 3");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(3);
        items.Select(i => i.Metadata["ext"]).Should().ContainEquivalentOf(".txt").And.ContainEquivalentOf(".md");
    }

    [Fact]
    public async Task WhenMineNestedDirectories_ExpectRecursiveDiscovery()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-nested-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("root.txt", "Root")
                .AddDirectory("subdir1")
                .AddTextFile("subdir1/sub1.txt", "Subdir 1")
                .AddDirectory("subdir1/nested")
                .AddTextFile("subdir1/nested/deep.txt", "Deep");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(3);
        items.Select(i => i.Metadata.TryGetValue("path", out var p) ? p?.ToString() : null)
            .Should().Contain(p => p != null && p.Contains("nested"));
    }

    [Fact]
    public async Task WhenMineEmptyDirectory_ExpectNoItems()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-empty-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        Directory.CreateDirectory(testDir);
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenMineMarkdownFiles_ExpectContentPreserved()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-md-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var mdContent = @"# Header
## Subheader

This is paragraph content.
- List item 1
- List item 2";
        
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddMarkdownFile("readme.md", mdContent);
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Content.Should().Contain("Header");
        items[0].Content.Should().Contain("List item");
        items[0].Metadata["ext"].Should().Be(".md");
    }

    [Fact]
    public async Task WhenMineJsonFile_ExpectValidJsonPreserved()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-json-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var jsonContent = """{"name": "test", "value": 42, "items": ["a", "b"]}""";
        
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddJsonFile("data.json", jsonContent);
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Content.Should().Contain("test");
        items[0].Content.Should().Contain("42");
    }

    [Fact]
    public async Task WhenMineWithFileExtensionFilter_ExpectMetadataCorrect()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-filter-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("doc.txt", "Text")
                .AddTextFile("code.cs", "public class")
                .AddTextFile("config.json", "{}");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(3);
        items.Select(i => i.Metadata["ext"]).Should().Contain(".txt").And.Contain(".cs").And.Contain(".json");
    }

    [Fact]
    public async Task WhenMineLargeFile_ExpectContentExtracted()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-large-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var largeContent = string.Join("\n", Enumerable.Range(0, 1000).Select(i => $"Line {i}: This is content"));
        
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("large.txt", largeContent);
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Content.Should().NotBeEmpty("Content should be extracted");
        items[0].Metadata["size"].Should().NotBeNull();
    }

    [Fact]
    public async Task WhenMineWithMetadataCollection_ExpectHashComputed()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-hash-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("test.txt", "Consistent content");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items1 = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items1.Should().HaveCount(1);
        items1[0].Metadata.Should().ContainKey("sha256_8", "File hash should be computed for deduplication");
    }

    [Fact]
    public async Task WhenMineMultipleCallsSameFile_ExpectConsistentResults()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-consistent-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("file.txt", "Consistent content");
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items1 = await miner.MineAsync(ctx).ToListAsync();
        var items2 = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items1.Should().HaveCount(items2.Count);
        items1[0].Content.Should().Be(items2[0].Content);
        items1[0].Metadata["sha256_8"].Should().Be(items2[0].Metadata["sha256_8"]);
    }

    [Fact]
    public async Task WhenMineTextWithSpecialCharacters_ExpectPreserved()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"mine-special-{Guid.NewGuid()}");
        _dirsToClean.Add(testDir);
        var specialContent = "Text with émojis 🚀 and spëcial çharacters: \t\n";
        
        var builder = new TestDirectoryBuilder(testDir);
        builder.AddTextFile("special.txt", specialContent);
        
        var miner = new FileSystemMiner();
        var ctx = new MinerContext(testDir, null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Content.Should().Contain("émojis");
        items[0].Content.Should().Contain("spëcial");
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
