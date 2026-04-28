using FluentAssertions;
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Tests for embedder type selection and pluggability.
/// </summary>
public sealed class EmbedderTypeSelectionTests
{
    [Fact]
    public void EmbedderOptions_DefaultsToLocal()
    {
        // Arrange & Act
        var options = new EmbedderOptions();

        // Assert
        options.Type.Should().Be(EmbedderType.Local);
        options.Model.Should().Be("sentence-transformers/all-MiniLM-L6-v2");
    }

    [Fact]
    public void EmbedderOptions_CanSetOpenAIType()
    {
        // Arrange & Act
        var options = new EmbedderOptions
        {
            Type = EmbedderType.OpenAI,
            Model = "text-embedding-3-small",
            ApiKey = "test-key"
        };

        // Assert
        options.Type.Should().Be(EmbedderType.OpenAI);
        options.Model.Should().Be("text-embedding-3-small");
        options.ApiKey.Should().Be("test-key");
    }

    [Fact]
    public void EmbedderOptions_CanSetAzureOpenAIType()
    {
        // Arrange & Act
        var options = new EmbedderOptions
        {
            Type = EmbedderType.AzureOpenAI,
            Model = "text-embedding-ada-002",
            ApiKey = "test-key",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "test-deployment"
        };

        // Assert
        options.Type.Should().Be(EmbedderType.AzureOpenAI);
        options.DeploymentName.Should().Be("test-deployment");
        options.Endpoint.Should().Be("https://test.openai.azure.com");
    }

    [Fact]
    public void AddMemPalaceAi_WithLocalType_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.Local;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var embedder = provider.GetService<IEmbedder>();
        embedder.Should().NotBeNull();
        embedder!.ModelIdentity.Should().Contain("local:");
    }

    [Fact]
    public void AddMemPalaceAi_WithOpenAIType_ThrowsWithoutApiKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.OpenAI;
            options.Model = "text-embedding-3-small";
            // No API key
        });
        var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is required*");
    }

    [Fact]
    public void AddMemPalaceAi_WithAzureOpenAIType_ThrowsWithoutEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.AzureOpenAI;
            options.ApiKey = "test-key";
            options.DeploymentName = "test-deployment";
            // No endpoint
        });
        var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint is required*");
    }

    [Fact]
    public void AddMemPalaceAi_WithAzureOpenAIType_ThrowsWithoutDeploymentName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.AzureOpenAI;
            options.ApiKey = "test-key";
            options.Endpoint = "https://test.openai.azure.com";
            // No deployment name
        });
        var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment name is required*");
    }

    [Fact]
    public void EmbedderType_HasCorrectEnumValues()
    {
        // Assert
        var values = Enum.GetValues<EmbedderType>();
        values.Should().Contain(EmbedderType.Local);
        values.Should().Contain(EmbedderType.OpenAI);
        values.Should().Contain(EmbedderType.AzureOpenAI);
        values.Should().HaveCount(3);
    }

    [Fact]
    public void AddMemPalaceAi_RegistersIEmbedderAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi();

        // Act
        var provider = services.BuildServiceProvider();
        var embedder1 = provider.GetService<IEmbedder>();
        var embedder2 = provider.GetService<IEmbedder>();

        // Assert
        embedder1.Should().NotBeNull();
        embedder2.Should().NotBeNull();
        embedder1.Should().BeSameAs(embedder2); // Singleton
    }

    [Fact]
    public void AddMemPalaceAi_RegistersIEmbeddingGeneratorAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi();

        // Act
        var provider = services.BuildServiceProvider();
        var generator1 = provider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        var generator2 = provider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Assert
        generator1.Should().NotBeNull();
        generator2.Should().NotBeNull();
        generator1.Should().BeSameAs(generator2); // Singleton
    }

    [Fact]
    public void EmbedderOptions_MaxSequenceLengthDefaultsTo256()
    {
        // Arrange & Act
        var options = new EmbedderOptions();

        // Assert
        options.MaxSequenceLength.Should().Be(256);
    }

    [Fact]
    public void EmbedderOptions_CanCustomizeMaxSequenceLength()
    {
        // Arrange & Act
        var options = new EmbedderOptions
        {
            MaxSequenceLength = 512
        };

        // Assert
        options.MaxSequenceLength.Should().Be(512);
    }

    [Fact]
    public void AddMemPalaceAi_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Type.Should().Be(EmbedderType.Local);
        options.Model.Should().Be("sentence-transformers/all-MiniLM-L6-v2");
        options.MaxSequenceLength.Should().Be(256);
    }

    [Fact]
    public void AddMemPalaceAi_ConfigurationOverridesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi(options =>
        {
            options.Type = EmbedderType.Local;
            options.Model = "custom-model";
            options.MaxSequenceLength = 1024;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Model.Should().Be("custom-model");
        options.MaxSequenceLength.Should().Be(1024);
    }

    [Fact]
    public void EmbedderOptions_ObsoleteProvider_StillWorks()
    {
        // Arrange & Act
        #pragma warning disable CS0618 // Type or member is obsolete
        var options = new EmbedderOptions
        {
            Provider = "Local"
        };
        #pragma warning restore CS0618

        // Assert
        #pragma warning disable CS0618
        options.Provider.Should().Be("Local");
        #pragma warning restore CS0618
    }

    [Fact]
    public void MeaiEmbedder_WithLocalProvider_HasCorrectModelIdentity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi();
        var provider = services.BuildServiceProvider();

        // Act
        var embedder = provider.GetRequiredService<IEmbedder>();

        // Assert
        embedder.ModelIdentity.Should().Contain("local:");
    }

    [Fact]
    public void AddMemPalaceAi_MultipleRegistrations_UseLastConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options => options.Model = "model1");
        services.AddMemPalaceAi(options => options.Model = "model2");

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Model.Should().Be("model2");
    }
}
