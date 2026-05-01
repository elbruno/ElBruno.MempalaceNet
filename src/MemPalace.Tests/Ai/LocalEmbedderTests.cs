using FluentAssertions;
using MemPalace.Ai.Embedding;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Unit tests for LocalEmbedder.
/// Tests the ONNX-based local embedder wrapper.
/// </summary>
public sealed class LocalEmbedderTests
{
    [Fact]
    public void Constructor_WithDefaultModel_CreatesEmbedder()
    {
        // Act
        using var embedder = new LocalEmbedder();

        // Assert
        embedder.Should().NotBeNull();
        embedder.ModelIdentity.Should().Contain("local:");
        embedder.ModelIdentity.Should().Contain("all-MiniLM-L6-v2");
    }

    [Fact(Skip = "Requires custom model to be downloaded first")]
    public void Constructor_WithCustomModel_CreatesEmbedder()
    {
        // Arrange
        const string customModel = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2";

        // Act
        using var embedder = new LocalEmbedder(modelName: customModel);

        // Assert
        embedder.Should().NotBeNull();
        embedder.ModelIdentity.Should().Contain("local:");
        embedder.ModelIdentity.Should().Contain(customModel);
    }

    [Fact]
    public void Constructor_WithNullModelName_ThrowsArgumentException()
    {
        // Act
        var act = () => new LocalEmbedder(modelName: null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Model name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithEmptyModelName_ThrowsArgumentException()
    {
        // Act
        var act = () => new LocalEmbedder(modelName: "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Model name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithZeroMaxSequenceLength_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new LocalEmbedder(maxSequenceLength: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Max sequence length must be positive*");
    }

    [Fact]
    public void Constructor_WithNegativeMaxSequenceLength_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new LocalEmbedder(maxSequenceLength: -10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Max sequence length must be positive*");
    }

    [Fact]
    public void ModelIdentity_HasCorrectFormat()
    {
        // Arrange
        const string modelName = "sentence-transformers/all-MiniLM-L6-v2";
        using var embedder = new LocalEmbedder(modelName: modelName);

        // Act
        var identity = embedder.ModelIdentity;

        // Assert
        identity.Should().Be($"local:{modelName}");
    }

    [Fact]
    public void Dimensions_BeforeFirstEmbed_ThrowsInvalidOperationException()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var act = () => embedder.Dimensions;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Dimensions not yet known*");
    }

    [Fact]
    public async Task Dimensions_AfterFirstEmbed_ReturnsDimensions()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        await embedder.EmbedAsync(new[] { "test" });
        var dimensions = embedder.Dimensions;

        // Assert
        dimensions.Should().BeGreaterThan(0);
        dimensions.Should().Be(384); // all-MiniLM-L6-v2 dimension
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var result = await embedder.EmbedAsync(Array.Empty<string>());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedAsync_WithNullList_ReturnsEmpty()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var result = await embedder.EmbedAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedAsync_WithSingleText_ReturnsEmbedding()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var result = await embedder.EmbedAsync(new[] { "hello world" });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Length.Should().Be(384); // all-MiniLM-L6-v2
    }

    [Fact]
    public async Task EmbedAsync_WithMultipleTexts_ReturnsEmbeddings()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var result = await embedder.EmbedAsync(new[]
        {
            "first text",
            "second text",
            "third text"
        });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(embedding => embedding.Length.Should().Be(384));
    }

    [Fact]
    public async Task EmbedAsync_WithSameText_ReturnsSameEmbedding()
    {
        // Arrange
        using var embedder = new LocalEmbedder();
        const string text = "consistent text";

        // Act
        var result1 = await embedder.EmbedAsync(new[] { text });
        var result2 = await embedder.EmbedAsync(new[] { text });

        // Assert
        result1[0].Span.ToArray().Should().Equal(result2[0].Span.ToArray());
    }

    [Fact]
    public async Task EmbedAsync_EmbeddingsAreNonZero()
    {
        // Arrange
        using var embedder = new LocalEmbedder();

        // Act
        var result = await embedder.EmbedAsync(new[] { "test embedding quality" });

        // Assert - embeddings should be non-zero (ElBruno.LocalEmbeddings doesn't normalize by default)
        var embedding = result[0].Span.ToArray();
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        magnitude.Should().BeGreaterThan(0.0f); // Non-zero vector
    }

    [Fact]
    public async Task EmbedAsync_CancellationToken_Respected()
    {
        // Arrange
        using var embedder = new LocalEmbedder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await embedder.EmbedAsync(new[] { "test" }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var embedder = new LocalEmbedder();

        // Act
        embedder.Dispose();

        // Assert - no exception thrown
        embedder.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var embedder = new LocalEmbedder();

        // Act
        embedder.Dispose();
        embedder.Dispose();

        // Assert - no exception thrown
        embedder.Should().NotBeNull();
    }
}
