using FluentAssertions;
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Unit tests for EmbedderFactory.
/// Tests embedder creation from options and custom instances.
/// </summary>
public sealed class EmbedderFactoryTests
{
    [Fact]
    public void Create_WithLocalType_ReturnsLocalEmbedder()
    {
        // Arrange
        var options = new EmbedderOptions
        {
            Type = EmbedderType.Local,
            Model = "sentence-transformers/all-MiniLM-L6-v2"
        };

        // Act
        var embedder = EmbedderFactory.Create(options);

        // Assert
        embedder.Should().NotBeNull();
        embedder.Should().BeOfType<LocalEmbedder>();
        embedder.ModelIdentity.Should().Contain("local:");
    }

    [Fact]
    public void Create_WithOpenAIType_ThrowsWithoutApiKey()
    {
        // Arrange
        var options = new EmbedderOptions
        {
            Type = EmbedderType.OpenAI,
            Model = "text-embedding-3-small"
            // No API key
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is required*");
    }

    [Fact]
    public void Create_WithAzureOpenAIType_ThrowsWithoutEndpoint()
    {
        // Arrange
        var options = new EmbedderOptions
        {
            Type = EmbedderType.AzureOpenAI,
            ApiKey = "test-key",
            DeploymentName = "test-deployment"
            // No endpoint
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint is required*");
    }

    [Fact]
    public void Create_WithAzureOpenAIType_ThrowsWithoutDeploymentName()
    {
        // Arrange
        var options = new EmbedderOptions
        {
            Type = EmbedderType.AzureOpenAI,
            ApiKey = "test-key",
            Endpoint = "https://test.openai.azure.com"
            // No deployment name
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment name is required*");
    }

    [Fact]
    public void Create_WithCustomEmbedder_ReturnsCustomEmbedder()
    {
        // Arrange
        var customEmbedder = new TestCustomEmbedder();
        var options = new EmbedderOptions
        {
            CustomEmbedder = customEmbedder
        };

        // Act
        var embedder = EmbedderFactory.Create(options);

        // Assert
        embedder.Should().BeSameAs(customEmbedder);
    }

    [Fact]
    public void Create_WithCustomEmbedderNullModelIdentity_ThrowsArgumentException()
    {
        // Arrange
        var customEmbedder = new InvalidCustomEmbedder(modelIdentity: null!, dimensions: 128);
        var options = new EmbedderOptions
        {
            CustomEmbedder = customEmbedder
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ModelIdentity cannot be null or empty*");
    }

    [Fact]
    public void Create_WithCustomEmbedderEmptyModelIdentity_ThrowsArgumentException()
    {
        // Arrange
        var customEmbedder = new InvalidCustomEmbedder(modelIdentity: "", dimensions: 128);
        var options = new EmbedderOptions
        {
            CustomEmbedder = customEmbedder
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ModelIdentity cannot be null or empty*");
    }

    [Fact]
    public void Create_WithCustomEmbedderZeroDimensions_ThrowsArgumentException()
    {
        // Arrange
        var customEmbedder = new InvalidCustomEmbedder(modelIdentity: "test", dimensions: 0);
        var options = new EmbedderOptions
        {
            CustomEmbedder = customEmbedder
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Dimensions must be positive*");
    }

    [Fact]
    public void Create_WithCustomEmbedderNegativeDimensions_ThrowsArgumentException()
    {
        // Arrange
        var customEmbedder = new InvalidCustomEmbedder(modelIdentity: "test", dimensions: -10);
        var options = new EmbedderOptions
        {
            CustomEmbedder = customEmbedder
        };

        // Act
        var act = () => EmbedderFactory.Create(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Dimensions must be positive*");
    }

    [Fact]
    public void Create_CustomEmbedderPrecedesType()
    {
        // Arrange - set both CustomEmbedder and Type
        var customEmbedder = new TestCustomEmbedder();
        var options = new EmbedderOptions
        {
            Type = EmbedderType.Local,  // Should be ignored
            CustomEmbedder = customEmbedder
        };

        // Act
        var embedder = EmbedderFactory.Create(options);

        // Assert - CustomEmbedder should take precedence
        embedder.Should().BeSameAs(customEmbedder);
        embedder.ModelIdentity.Should().Be("test-custom-embedder-v1");
    }

    [Fact]
    public void Create_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => EmbedderFactory.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateCustom_WithValidEmbedder_ReturnsEmbedder()
    {
        // Arrange
        var customEmbedder = new TestCustomEmbedder();

        // Act
        var embedder = EmbedderFactory.CreateCustom(customEmbedder);

        // Assert
        embedder.Should().BeSameAs(customEmbedder);
    }

    [Fact]
    public void CreateCustom_WithNullEmbedder_ThrowsArgumentNullException()
    {
        // Act
        var act = () => EmbedderFactory.CreateCustom(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateCustom_WithInvalidEmbedder_ThrowsArgumentException()
    {
        // Arrange
        var customEmbedder = new InvalidCustomEmbedder(modelIdentity: null!, dimensions: 128);

        // Act
        var act = () => EmbedderFactory.CreateCustom(customEmbedder);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // Test helper: valid custom embedder
    private sealed class TestCustomEmbedder : ICustomEmbedder
    {
        public string ProviderName => "test";
        public string ModelIdentity => "test-custom-embedder-v1";
        public int Dimensions => 128;
        public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
        {
            { "provider", "test" },
            { "model", "custom-embedder-v1" }
        };

        public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            var results = texts.Select(_ => new ReadOnlyMemory<float>(new float[Dimensions])).ToList();
            return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
        }
    }

    // Test helper: invalid custom embedder for testing validation
    private sealed class InvalidCustomEmbedder : ICustomEmbedder
    {
        private readonly string _modelIdentity;
        private readonly int _dimensions;

        public InvalidCustomEmbedder(string modelIdentity, int dimensions)
        {
            _modelIdentity = modelIdentity;
            _dimensions = dimensions;
        }

        public string ProviderName => "invalid";
        public string ModelIdentity => _modelIdentity;
        public int Dimensions => _dimensions;
        public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>();

        public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
