using MemPalace.Ai.Summarization;
using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using Xunit;

namespace MemPalace.Tests.Ai.Summarization;

public sealed class MemorySummarizerTests
{
    [Fact]
    public async Task NoOpMemorySummarizer_ReturnsNull()
    {
        // Arrange
        var summarizer = new NoOpMemorySummarizer();
        var memories = new GetResult(
            Ids: new[] { "1", "2" },
            Documents: new[] { "Memory 1", "Memory 2" },
            Metadatas: new[] 
            {
                new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>,
                new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>
            }
        );

        // Act
        var result = await summarizer.SummarizeAsync(memories);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LLMMemorySummarizer_WithEmptyMemories_ReturnsNull()
    {
        // Arrange
        var mockChatClient = new MockChatClient("Test summary");
        var summarizer = new LLMMemorySummarizer(mockChatClient);
        var emptyMemories = new GetResult(
            Ids: Array.Empty<string>(),
            Documents: Array.Empty<string>(),
            Metadatas: Array.Empty<IReadOnlyDictionary<string, object?>>()
        );

        // Act
        var result = await summarizer.SummarizeAsync(emptyMemories);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LLMMemorySummarizer_WithMemories_CallsChatClient()
    {
        // Arrange
        var expectedSummary = "• Implemented BM25 search\n• Added knowledge graph\n• Completed Phase 6";
        var mockChatClient = new MockChatClient(expectedSummary);
        var summarizer = new LLMMemorySummarizer(mockChatClient);
        var memories = new GetResult(
            Ids: new[] { "1", "2", "3" },
            Documents: new[] 
            { 
                "Implemented BM25 keyword search",
                "Added temporal knowledge graph",
                "Completed Phase 6 tasks"
            },
            Metadatas: new[]
            {
                new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>,
                new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>,
                new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>
            }
        );

        // Act
        var result = await summarizer.SummarizeAsync(memories);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSummary, result);
        Assert.True(mockChatClient.WasCalled);
        Assert.Equal(2, mockChatClient.LastMessages!.Count); // System + User
    }

    [Fact]
    public async Task LLMMemorySummarizer_LimitsTo50Memories()
    {
        // Arrange
        var mockChatClient = new MockChatClient("Summary of 50 memories");
        var summarizer = new LLMMemorySummarizer(mockChatClient);
        
        // Create 60 memories
        var ids = Enumerable.Range(1, 60).Select(i => i.ToString()).ToArray();
        var docs = Enumerable.Range(1, 60).Select(i => $"Memory {i}").ToArray();
        var metas = Enumerable.Range(1, 60)
            .Select(_ => new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>)
            .ToArray();
        
        var memories = new GetResult(ids, docs, metas);

        // Act
        var result = await summarizer.SummarizeAsync(memories);

        // Assert
        Assert.NotNull(result);
        // Verify that only 50 memories were included in the prompt
        var userMessage = mockChatClient.LastMessages![1].Text;
        Assert.Contains("50. Memory 50", userMessage);
        Assert.DoesNotContain("51. Memory 51", userMessage);
    }

    [Fact]
    public async Task LLMMemorySummarizer_HandlesExceptionGracefully()
    {
        // Arrange
        var mockChatClient = new MockChatClient(throwException: true);
        var summarizer = new LLMMemorySummarizer(mockChatClient);
        var memories = new GetResult(
            Ids: new[] { "1" },
            Documents: new[] { "Memory 1" },
            Metadatas: new[] { new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?> }
        );

        // Act
        var result = await summarizer.SummarizeAsync(memories);

        // Assert
        Assert.Null(result); // Graceful degradation
    }

    [Fact]
    public void LLMMemorySummarizer_ThrowsOnNullChatClient()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LLMMemorySummarizer(null!));
    }

    // Mock IChatClient for testing
    private sealed class MockChatClient : IChatClient
    {
        private readonly string _response;
        private readonly bool _throwException;

        public bool WasCalled { get; private set; }
        public IList<ChatMessage>? LastMessages { get; private set; }

        public MockChatClient(string response = "", bool throwException = false)
        {
            _response = response;
            _throwException = throwException;
        }

        public ChatClientMetadata Metadata => new("mock-client");

        public async Task<ChatResponse> GetResponseAsync(
            IList<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastMessages = chatMessages;

            if (_throwException)
            {
                throw new InvalidOperationException("Mock LLM error");
            }

            await Task.CompletedTask;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, _response));
        }

        public IAsyncEnumerable<StreamingChatCompletionUpdate> GetStreamingResponseAsync(
            IList<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public TService? GetService<TService>(object? key = null) where TService : class
        {
            return null;
        }

        public void Dispose() { }
    }
}
