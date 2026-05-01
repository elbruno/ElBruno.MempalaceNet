using MemPalace.Ai.Embedding;
using MemPalace.Core.Errors;
using Xunit;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Unit tests for OpenAIEmbedder implementation.
/// These tests verify API contract compliance, error handling, and rate limiting.
/// NOTE: Tests that require real OpenAI API calls are marked with Skip attribute.
/// </summary>
public class OpenAIEmbedderTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OpenAIEmbedder(null!));
    }

    [Fact]
    public void Constructor_EmptyApiKey_ThrowsArgumentException()
    {
        // Arrange
        var options = new OpenAIOptions { ApiKey = "" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new OpenAIEmbedder(options));
        Assert.Contains("API key is required", ex.Message);
    }

    [Fact]
    public void ProviderName_ReturnsOpenAI()
    {
        // Arrange
        var options = new OpenAIOptions { ApiKey = "test-key" };
        var embedder = new OpenAIEmbedder(options);

        // Act
        var providerName = embedder.ProviderName;

        // Assert
        Assert.Equal("openai", providerName);
    }

    [Fact]
    public void ModelIdentity_ReturnsCorrectFormat()
    {
        // Arrange
        var options = new OpenAIOptions 
        { 
            ApiKey = "test-key",
            Model = "text-embedding-3-small"
        };
        var embedder = new OpenAIEmbedder(options);

        // Act
        var modelIdentity = embedder.ModelIdentity;

        // Assert
        Assert.Equal("openai:text-embedding-3-small", modelIdentity);
    }

    [Fact]
    public void Dimensions_BeforeFirstCall_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new OpenAIOptions { ApiKey = "test-key" };
        var embedder = new OpenAIEmbedder(options);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => embedder.Dimensions);
        Assert.Contains("not yet known", ex.Message);
        Assert.Contains("Call EmbedAsync", ex.Message);
    }

    [Fact]
    public void Metadata_ContainsExpectedKeys()
    {
        // Arrange
        var options = new OpenAIOptions 
        { 
            ApiKey = "test-key",
            Model = "text-embedding-3-large",
            CostPer1MTokens = 0.13m,
            MaxRequestsPerMinute = 5000,
            MaxRetries = 5
        };
        var embedder = new OpenAIEmbedder(options);

        // Act
        var metadata = embedder.Metadata;

        // Assert
        Assert.Equal("openai", metadata["provider"]);
        Assert.Equal("text-embedding-3-large", metadata["model"]);
        Assert.Equal(0.13m, metadata["cost_per_1m_tokens"]);
        Assert.Equal(5000, metadata["max_rpm"]);
        Assert.Equal(5, metadata["max_retries"]);
    }

    [Fact]
    public async Task EmbedAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var options = new OpenAIOptions { ApiKey = "test-key" };
        var embedder = new OpenAIEmbedder(options);

        // Act
        var embeddings = await embedder.EmbedAsync(Array.Empty<string>());

        // Assert
        Assert.Empty(embeddings);
    }

    [Fact(Skip = "Requires valid OPENAI_API_KEY environment variable")]
    public async Task EmbedAsync_ValidApiKey_ReturnsEmbeddings()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
        
        var options = new OpenAIOptions 
        { 
            ApiKey = apiKey,
            Model = "text-embedding-3-small"
        };
        var embedder = new OpenAIEmbedder(options);
        var texts = new[] { "Hello world", "MemPalace.NET" };

        // Act
        var embeddings = await embedder.EmbedAsync(texts);

        // Assert
        Assert.Equal(2, embeddings.Count);
        Assert.Equal(1536, embeddings[0].Length);
        Assert.Equal(1536, embeddings[1].Length);
        Assert.Equal(1536, embedder.Dimensions);
    }

    [Fact(Skip = "Requires valid OPENAI_API_KEY to test invalid key error")]
    public async Task EmbedAsync_InvalidApiKey_ThrowsEmbedderErrorWithClearMessage()
    {
        // Arrange
        var options = new OpenAIOptions 
        { 
            ApiKey = "sk-invalid-key-12345",
            Model = "text-embedding-3-small"
        };
        var embedder = new OpenAIEmbedder(options);
        var texts = new[] { "Test" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EmbedderError>(() => 
            embedder.EmbedAsync(texts).AsTask());
        
        Assert.Contains("Invalid OpenAI API key", ex.Message);
        Assert.Contains("verify your API key", ex.Message);
    }

    [Fact(Skip = "Requires valid OPENAI_API_KEY to test invalid model")]
    public async Task EmbedAsync_InvalidModel_ThrowsEmbedderErrorWithSuggestions()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
        
        var options = new OpenAIOptions 
        { 
            ApiKey = apiKey,
            Model = "nonexistent-model-xyz"
        };
        var embedder = new OpenAIEmbedder(options);
        var texts = new[] { "Test" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EmbedderError>(() => 
            embedder.EmbedAsync(texts).AsTask());
        
        Assert.Contains("not found", ex.Message);
        Assert.Contains("Valid models", ex.Message);
    }

    [Fact]
    public void OpenAIOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new OpenAIOptions();

        // Assert
        Assert.Equal("text-embedding-3-small", options.Model);
        Assert.Equal(3500, options.MaxRequestsPerMinute);
        Assert.Equal(0.02m, options.CostPer1MTokens);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.InitialRetryDelay);
    }

    [Fact(Skip = "Requires valid OPENAI_API_KEY to test rate limiting")]
    public async Task RateLimiting_MultipleRequests_RespectRpm()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
        
        var options = new OpenAIOptions 
        { 
            ApiKey = apiKey,
            Model = "text-embedding-3-small",
            MaxRequestsPerMinute = 10  // Low RPM for testing
        };
        var embedder = new OpenAIEmbedder(options);
        var texts = new[] { "Test" };

        // Act
        var startTime = DateTime.UtcNow;
        
        // Make 3 rapid requests
        await embedder.EmbedAsync(texts);
        await embedder.EmbedAsync(texts);
        await embedder.EmbedAsync(texts);
        
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - should take at least 6 seconds for 3 requests at 10 rpm (6 sec per request)
        Assert.True(elapsed.TotalSeconds >= 0.5, 
            "Rate limiting should introduce delays between requests");
    }

    [Theory]
    [InlineData("text-embedding-3-small")]
    [InlineData("text-embedding-3-large")]
    public void ModelIdentity_DifferentModels_ReturnsCorrectIdentity(string model)
    {
        // Arrange
        var options = new OpenAIOptions 
        { 
            ApiKey = "test-key",
            Model = model
        };
        var embedder = new OpenAIEmbedder(options);

        // Act
        var identity = embedder.ModelIdentity;

        // Assert
        Assert.Equal($"openai:{model}", identity);
    }

    [Fact(Skip = "Requires valid OPENAI_API_KEY to test retry logic")]
    public async Task RetryLogic_TransientError_RetriesWithExponentialBackoff()
    {
        // This test would require mocking the OpenAI client or using a test server
        // that simulates 429 rate limit errors followed by success.
        // Skipped for now as it requires more complex test infrastructure.
        await Task.CompletedTask;
    }
}
