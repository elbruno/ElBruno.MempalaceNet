using FluentAssertions;
using MemPalace.Ai.Embedding;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace MemPalace.Tests.Ai;

public sealed class MeaiEmbedderTests
{
    [Fact]
    public void Constructor_WithNullGenerator_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MeaiEmbedder(null!, "ollama", "model");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("generator");
    }

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();

        // Act
        var act = () => new MeaiEmbedder(generator, null!, "model");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("providerName");
    }

    [Fact]
    public void Constructor_WithNullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();

        // Act
        var act = () => new MeaiEmbedder(generator, "ollama", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("modelName");
    }

    [Fact]
    public void ModelIdentity_CombinesProviderAndModel()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var embedder = new MeaiEmbedder(generator, "ollama", "nomic-embed-text");

        // Act
        var identity = embedder.ModelIdentity;

        // Assert
        identity.Should().Be("ollama:nomic-embed-text");
    }

    [Fact]
    public void Dimensions_BeforeFirstEmbed_ThrowsInvalidOperationException()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        var act = () => embedder.Dimensions;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Dimensions not yet known*");
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        var result = await embedder.EmbedAsync(Array.Empty<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedAsync_WithSingleText_ReturnsEmbedding()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var expectedVector = new float[] { 0.1f, 0.2f, 0.3f };
        var embedding = new Embedding<float>(expectedVector);
        
        generator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([embedding]));

        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        var result = await embedder.EmbedAsync(new[] { "test text" });

        // Assert
        result.Should().HaveCount(1);
        result[0].ToArray().Should().BeEquivalentTo(expectedVector);
        embedder.Dimensions.Should().Be(3);
    }

    [Fact]
    public async Task EmbedAsync_WithMultipleTexts_ReturnsBatchedEmbeddings()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var vector1 = new float[] { 0.1f, 0.2f };
        var vector2 = new float[] { 0.3f, 0.4f };
        var vector3 = new float[] { 0.5f, 0.6f };
        
        var embeddings = new GeneratedEmbeddings<Embedding<float>>([
            new Embedding<float>(vector1),
            new Embedding<float>(vector2),
            new Embedding<float>(vector3)
        ]);

        generator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(embeddings);

        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        var result = await embedder.EmbedAsync(new[] { "text1", "text2", "text3" });

        // Assert
        result.Should().HaveCount(3);
        result[0].ToArray().Should().BeEquivalentTo(vector1);
        result[1].ToArray().Should().BeEquivalentTo(vector2);
        result[2].ToArray().Should().BeEquivalentTo(vector3);
        embedder.Dimensions.Should().Be(2);
    }

    [Fact]
    public async Task EmbedAsync_InfersDimensionsFromFirstCall()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var vector = new float[] { 1f, 2f, 3f, 4f, 5f };
        var embedding = new Embedding<float>(vector);
        
        generator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([embedding]));

        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        await embedder.EmbedAsync(new[] { "test" });

        // Assert
        embedder.Dimensions.Should().Be(5);
    }

    [Fact]
    public async Task EmbedAsync_ReadOnlyMemoryValues_RoundTripCorrectly()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var originalValues = new float[] { 0.123456f, 0.789012f, 0.345678f };
        var embedding = new Embedding<float>(originalValues);
        
        generator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([embedding]));

        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        var result = await embedder.EmbedAsync(new[] { "test" });

        // Assert
        var retrievedValues = result[0].ToArray();
        retrievedValues.Should().HaveCount(3);
        for (int i = 0; i < originalValues.Length; i++)
        {
            retrievedValues[i].Should().BeApproximately(originalValues[i], 0.000001f);
        }
    }

    [Fact]
    public async Task EmbedAsync_PassesCancellationToken()
    {
        // Arrange
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var cts = new CancellationTokenSource();
        var embedding = new Embedding<float>(new float[] { 1f });
        
        generator.GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([embedding]));

        var embedder = new MeaiEmbedder(generator, "ollama", "model");

        // Act
        await embedder.EmbedAsync(new[] { "test" }, cts.Token);

        // Assert
        await generator.Received(1).GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions>(),
            cts.Token);
    }
}
