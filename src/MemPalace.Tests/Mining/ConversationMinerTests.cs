using FluentAssertions;
using MemPalace.Mining;

namespace MemPalace.Tests.Mining;

public sealed class ConversationMinerTests
{
    [Fact]
    public async Task MineAsync_JsonlFormat_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jsonl");
        
        try
        {
            var jsonl = """
                {"role": "user", "content": "Hello there"}
                {"role": "assistant", "content": "General Kenobi"}
                {"role": "user", "content": "You are a bold one", "timestamp": "2026-04-24T10:00:00Z"}
                """;
            await File.WriteAllTextAsync(tempFile, jsonl);

            var miner = new ConversationMiner();
            var ctx = new MinerContext(tempFile, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(3);
            items[0].Content.Should().Be("Hello there");
            items[0].Metadata["role"].Should().Be("user");
            items[0].Metadata["turn_index"].Should().Be(0);
            
            items[1].Content.Should().Be("General Kenobi");
            items[1].Metadata["role"].Should().Be("assistant");
            items[1].Metadata["turn_index"].Should().Be(1);
            
            items[2].Metadata.Should().ContainKey("timestamp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MineAsync_MarkdownFormat_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md");
        
        try
        {
            var markdown = """
                ## User
                What is the capital of France?

                ## Assistant
                The capital of France is Paris.

                ## User
                Thank you!
                """;
            await File.WriteAllTextAsync(tempFile, markdown);

            var miner = new ConversationMiner();
            var ctx = new MinerContext(tempFile, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(3);
            items[0].Content.Should().Contain("capital of France");
            items[0].Metadata["role"].Should().Be("user");
            items[0].Metadata["turn_index"].Should().Be(0);
            
            items[1].Content.Should().Contain("Paris");
            items[1].Metadata["role"].Should().Be("assistant");
            
            items[2].Content.Should().Contain("Thank you");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MineAsync_InvalidJsonl_SkipsBadLines()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jsonl");
        
        try
        {
            var jsonl = """
                {"role": "user", "content": "Valid line"}
                this is not valid json
                {"role": "assistant", "content": "Another valid line"}
                """;
            await File.WriteAllTextAsync(tempFile, jsonl);

            var miner = new ConversationMiner();
            var ctx = new MinerContext(tempFile, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(2);
            items[0].Content.Should().Be("Valid line");
            items[1].Content.Should().Be("Another valid line");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MineAsync_NonExistentFile_ReturnsEmpty()
    {
        // Arrange
        var miner = new ConversationMiner();
        var ctx = new MinerContext("/nonexistent/file.jsonl", null, new Dictionary<string, string?>());

        // Act
        var items = await miner.MineAsync(ctx).ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task MineAsync_AlternateRoleNames_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md");
        
        try
        {
            var markdown = """
                ## Human
                Hello

                ## AI
                Hi there
                """;
            await File.WriteAllTextAsync(tempFile, markdown);

            var miner = new ConversationMiner();
            var ctx = new MinerContext(tempFile, null, new Dictionary<string, string?>());

            // Act
            var items = await miner.MineAsync(ctx).ToListAsync();

            // Assert
            items.Should().HaveCount(2);
            items[0].Metadata["role"].Should().Be("human");
            items[1].Metadata["role"].Should().Be("ai");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
