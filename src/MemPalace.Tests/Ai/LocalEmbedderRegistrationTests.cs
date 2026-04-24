using FluentAssertions;
using MemPalace.Ai.Embedding;
using MemPalace.Core.Backends;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Tests for the Local embedder provider registration via AddMemPalaceAi.
/// These tests verify DI configuration without triggering model downloads.
/// </summary>
public sealed class LocalEmbedderRegistrationTests
{
    [Fact]
    public void AddMemPalaceAi_WithDefaultOptions_ConfiguresLocalProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Provider.Should().Be("Local");
        options.Model.Should().Be("sentence-transformers/all-MiniLM-L6-v2");
    }

    [Fact]
    public void AddMemPalaceAi_WithLocalProvider_ConfiguresDefaultModel()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi(options =>
        {
            options.Provider = "Local";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Provider.Should().Be("Local");
        options.Model.Should().Be("sentence-transformers/all-MiniLM-L6-v2");
    }

    [Fact]
    public void AddMemPalaceAi_WithLocalProviderAndCustomModel_ConfiguresCustomModel()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi(options =>
        {
            options.Provider = "Local";
            options.Model = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Provider.Should().Be("Local");
        options.Model.Should().Be("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2");
    }

    [Fact]
    public void AddMemPalaceAi_WithOllamaProvider_ConfiguresOllama()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMemPalaceAi(options =>
        {
            options.Provider = "Ollama";
            options.Model = "nomic-embed-text";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EmbedderOptions>>().Value;
        options.Provider.Should().Be("Ollama");
        options.Model.Should().Be("nomic-embed-text");
    }

    [Fact]
    public void AddMemPalaceAi_RegistersEmbeddingGeneratorFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options =>
        {
            options.Provider = "Ollama";  // Use Ollama to avoid model download
        });

        // Act & Assert
        services.Should().Contain(descriptor => 
            descriptor.ServiceType == typeof(IEmbeddingGenerator<string, Embedding<float>>));
    }

    [Fact]
    public void AddMemPalaceAi_RegistersIEmbedderFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemPalaceAi(options =>
        {
            options.Provider = "Ollama";  // Use Ollama to avoid model download
        });

        // Act & Assert
        services.Should().Contain(descriptor => 
            descriptor.ServiceType == typeof(IEmbedder));
    }
}
