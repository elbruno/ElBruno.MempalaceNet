using FluentAssertions;
using MemPalace.Mining;

namespace MemPalace.Tests.Mining;

public sealed class FileSystemMinerTests
{
    [Fact]
    public async Task MineAsync_EmptyDirectory_ReturnsNoItems()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var miner = new FileSystemMiner();
            var ctx = new MinerContext(tempDir, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MineAsync_TextFiles_ExtractsCorrectMetadata()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var testContent = "This is test content for mining.";
            var testFile = Path.Combine(tempDir, "test.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            var miner = new FileSystemMiner();
            var ctx = new MinerContext(tempDir, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(1);
            items[0].Content.Should().Be(testContent);
            items[0].Metadata.Should().ContainKey("path");
            items[0].Metadata.Should().ContainKey("ext");
            items[0].Metadata.Should().ContainKey("size");
            items[0].Metadata.Should().ContainKey("mtime");
            items[0].Metadata.Should().ContainKey("sha256_8");
            items[0].Metadata["ext"].Should().Be(".txt");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MineAsync_LargeFile_ChunksCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var testContent = new string('x', 5000); // 5000 chars, should create multiple chunks
            var testFile = Path.Combine(tempDir, "large.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            var miner = new FileSystemMiner();
            var ctx = new MinerContext(tempDir, null, new Dictionary<string, string?>
            {
                ["chunk_size"] = "2000",
                ["overlap"] = "200"
            });

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCountGreaterThan(1);
            items[0].Metadata.Should().ContainKey("chunk_index");
            items[0].Metadata["chunk_index"].Should().Be(0);
            items[1].Metadata["chunk_index"].Should().Be(1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MineAsync_BinaryFile_SkipsFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var binaryFile = Path.Combine(tempDir, "test.exe");
            await File.WriteAllBytesAsync(binaryFile, new byte[] { 0x4D, 0x5A }); // PE header

            var miner = new FileSystemMiner();
            var ctx = new MinerContext(tempDir, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MineAsync_WithGitignore_RespectsExclusions()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var ignoredDir = Path.Combine(tempDir, "node_modules");
        Directory.CreateDirectory(ignoredDir);
        
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "include.txt"), "include me");
            await File.WriteAllTextAsync(Path.Combine(ignoredDir, "exclude.txt"), "exclude me");
            await File.WriteAllTextAsync(Path.Combine(tempDir, ".gitignore"), "node_modules/");

            var miner = new FileSystemMiner();
            var ctx = new MinerContext(tempDir, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(1);
            items[0].Metadata["path"].Should().Be("include.txt");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
