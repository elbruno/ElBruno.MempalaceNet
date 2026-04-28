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
        
        var getResult = new GetResult(
            Ids: new[] { "m1", "m2", "m3" },
            Documents: new[] { "Doc 1", "Doc 2", "Doc 3" },
            Metadatas: new[]
            {
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-1) },
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-2) },
                new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow.AddHours(-3) }
            },
            Embeddings: null);
        
        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(getResult);

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
        
        var getResult = new GetResult(
            Ids: new[] { "m1" },
            Documents: new[] { "Doc 1" },
            Metadatas: new[] { new Dictionary<string, object?>() },
            Embeddings: null);
        
        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(getResult);

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
        
        var getResult = new GetResult(
            Ids: new[] { "m1", "m2" },
            Documents: new[] { "Doc 1", "Doc 2" },
            Metadatas: new[]
            {
                new Dictionary<string, object?> { ["wing"] = "code", ["timestamp"] = DateTime.UtcNow },
                new Dictionary<string, object?> { ["wing"] = "docs", ["timestamp"] = DateTime.UtcNow }
            },
            Embeddings: null);
        
        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(getResult);

        var chatClient = Substitute.For<IChatClient>();
        var chatResponse = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "This is a summary of recent activities."));
        chatClient.CompleteAsync(Arg.Any<string>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
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
        var getResult = new GetResult(
            Ids: new[] { "m1", "m2", "m3" },
            Documents: new[] { "Oldest", "Newest", "Middle" },
            Metadatas: new[]
            {
                new Dictionary<string, object?> { ["timestamp"] = now.AddHours(-3) }, // Oldest
                new Dictionary<string, object?> { ["timestamp"] = now }, // Newest
                new Dictionary<string, object?> { ["timestamp"] = now.AddHours(-1) }  // Middle
            },
            Embeddings: null);
        
        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(getResult);

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
        
        var getResult = new GetResult(
            Ids: new[] { "m1" },
            Documents: new[] { "Doc 1" },
            Metadatas: new[] { new Dictionary<string, object?>() },
            Embeddings: null);
        
        collection.GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<WhereClause?>(),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>()).Returns(getResult);

        var service = new WakeUpService();
        var whereClause = new WhereClause.Eq("wing", "code");

        // Act
        var result = await service.WakeUpAsync(collection, limit: 10, where: whereClause);

        // Assert
        await collection.Received(1).GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Is<WhereClause?>(w => w == whereClause),
            Arg.Any<int?>(),
            Arg.Any<int>(),
            Arg.Any<IncludeFields>(),
            Arg.Any<CancellationToken>());
    }
}
