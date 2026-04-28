using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Core.Services;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace MemPalace.Tests.Services;

public sealed class WakeUpServiceTests
{
    [Fact]
    public async Task WakeUpAsync_NoSummarize_ReturnsMemoriesOnly()
    {
        // Arrange
        var collection = Substitute.For<ICollection>();
        collection.CountAsync(Arg.Any<CancellationToken>()).Returns(100);
        
        var memories = new List<EmbeddedRecord>
        {
            new EmbeddedRecord("m1", "Doc 1",
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-1).Ticks },
                ReadOnlyMemory<float>.Empty),
            new EmbeddedRecord("m2", "Doc 2",
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-2).Ticks },
                ReadOnlyMemory<float>.Empty),
            new EmbeddedRecord("m3", "Doc 3",
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-3).Ticks },
                ReadOnlyMemory<float>.Empty)
        };
        
        collection.WakeUpAsync(
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(memories);

        var service = new WakeUpService();

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10, summarize: false);

        // Assert
        Assert.Equal(3, result.Memories.Count);
        Assert.Null(result.Summary);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal("m1", result.Memories[0].Id);
    }

    [Fact]
    public async Task WakeUpAsync_WithSummarize_NoChatClient_ReturnsUnavailableMessage()
    {
        // Arrange
        var collection = Substitute.For<ICollection>();
        collection.CountAsync(Arg.Any<CancellationToken>()).Returns(10);
        
        var memories = new List<EmbeddedRecord>
        {
            new EmbeddedRecord("m1", "Doc 1",
                new Dictionary<string, object?>(),
                ReadOnlyMemory<float>.Empty)
        };
        
        collection.WakeUpAsync(
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(memories);

        var service = new WakeUpService(chatClient: null);

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10, summarize: true);

        // Assert
        Assert.Single(result.Memories);
        Assert.Null(result.Summary); // No chat client, so no summary
    }

    [Fact]
    public async Task WakeUpAsync_WithSummarize_WithChatClient_ReturnsSummary()
    {
        // Arrange
        var collection = Substitute.For<ICollection>();
        collection.CountAsync(Arg.Any<CancellationToken>()).Returns(10);
        
        var memories = new List<EmbeddedRecord>
        {
            new EmbeddedRecord("m1", "Doc 1", 
                new Dictionary<string, object?> { ["wing"] = "code", ["timestamp"] = DateTime.UtcNow.Ticks },
                ReadOnlyMemory<float>.Empty),
            new EmbeddedRecord("m2", "Doc 2",
                new Dictionary<string, object?> { ["wing"] = "docs", ["timestamp"] = DateTime.UtcNow.Ticks },
                ReadOnlyMemory<float>.Empty)
        };
        
        collection.WakeUpAsync(
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(memories);

        var chatClient = Substitute.For<IChatClient>();
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "This is a summary of recent activities."));
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        var service = new WakeUpService(chatClient);

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10, summarize: true);

        // Assert
        Assert.Equal(2, result.Memories.Count);
        Assert.NotNull(result.Summary);
        Assert.Equal("This is a summary of recent activities.", result.Summary);
    }

    [Fact]
    public async Task WakeUpAsync_SortsMemoriesByTimestamp()
    {
        // Arrange
        var collection = Substitute.For<ICollection>();
        collection.CountAsync(Arg.Any<CancellationToken>()).Returns(3);
        
        var now = DateTime.UtcNow;
        // WakeUpAsync returns memories already sorted by timestamp DESC (newest first)
        var memories = new List<EmbeddedRecord>
        {
            new EmbeddedRecord("m2", "Newest",
                new Dictionary<string, object?> { ["timestamp"] = now.Ticks },
                ReadOnlyMemory<float>.Empty),
            new EmbeddedRecord("m3", "Middle",
                new Dictionary<string, object?> { ["timestamp"] = now.AddHours(-1).Ticks },
                ReadOnlyMemory<float>.Empty),
            new EmbeddedRecord("m1", "Oldest",
                new Dictionary<string, object?> { ["timestamp"] = now.AddHours(-3).Ticks },
                ReadOnlyMemory<float>.Empty)
        };
        
        collection.WakeUpAsync(
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(memories);

        var service = new WakeUpService();

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10);

        // Assert
        Assert.Equal("m2", result.Memories[0].Id); // Newest first
        Assert.Equal("m3", result.Memories[1].Id); // Middle second
        Assert.Equal("m1", result.Memories[2].Id); // Oldest last
    }

    [Fact]
    public async Task WakeUpAsync_WithWhereClause_PassesFilterToBackend()
    {
        // Arrange
        var collection = Substitute.For<ICollection>();
        collection.CountAsync(Arg.Any<CancellationToken>()).Returns(5);
        
        var memories = new List<EmbeddedRecord>
        {
            new EmbeddedRecord("m1", "Doc 1",
                new Dictionary<string, object?>(),
                ReadOnlyMemory<float>.Empty)
        };
        
        collection.WakeUpAsync(
            Arg.Any<int>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(memories);

        var service = new WakeUpService();
        var whereClause = new WhereClause.Eq("wing", "code");

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10, where: whereClause);

        // Assert
        await collection.Received(1).WakeUpAsync(
            Arg.Any<int>(),
            Arg.Is<WhereClause?>(w => w == whereClause),
            Arg.Any<DateTime?>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>());
    }
}
