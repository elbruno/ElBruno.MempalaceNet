using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// Dependency injection extensions for MemPalace.Ai.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MemPalace AI services (embedder) with the specified configuration.
    /// </summary>
    public static IServiceCollection AddMemPalaceAi(
        this IServiceCollection services,
        Action<EmbedderOptions>? configure = null)
    {
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<EmbedderOptions>(_ => { });
        }

        // Register the embedding generator factory
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;

            return options.Provider.ToLowerInvariant() switch
            {
                "ollama" => CreateOllamaGenerator(options),
                "openai" => CreateOpenAiGenerator(options),
                "azureopenai" => CreateAzureOpenAiGenerator(options),
                _ => throw new InvalidOperationException(
                    $"Unknown embedding provider: {options.Provider}. " +
                    "Supported: Ollama, OpenAI, AzureOpenAI.")
            };
        });

        // Register the IEmbedder adapter
        services.AddSingleton<IEmbedder>(sp =>
        {
            var generator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;
            return new MeaiEmbedder(generator, options.Provider, options.Model);
        });

        return services;
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaGenerator(
        EmbedderOptions options)
    {
        var client = new OllamaEmbeddingGenerator(
            new Uri(options.Endpoint),
            options.Model);
        return client;
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAiGenerator(
        EmbedderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "ApiKey is required for OpenAI provider.");
        }

        return new OpenAIEmbeddingGenerator(
            new OpenAI.OpenAIClient(options.ApiKey),
            options.Model);
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAiGenerator(
        EmbedderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "ApiKey is required for AzureOpenAI provider.");
        }

        var credential = new System.ClientModel.ApiKeyCredential(options.ApiKey);
        var clientOptions = new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint)
        };
        
        return new OpenAIEmbeddingGenerator(
            new OpenAI.OpenAIClient(credential, clientOptions),
            options.Model);
    }
}
