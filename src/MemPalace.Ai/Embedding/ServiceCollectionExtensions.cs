using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ElBruno.LocalEmbeddings.Extensions;

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

        // For Local provider, register via ElBruno.LocalEmbeddings directly
        // For other providers, register via factory
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;

            return options.Provider.ToLowerInvariant() switch
            {
                "local" => throw new InvalidOperationException(
                    "Local provider must be registered using AddLocalEmbeddings before AddMemPalaceAi. " +
                    "This will be fixed in the final implementation."),
                "ollama" => CreateOllamaGenerator(options),
                "openai" => CreateOpenAiGenerator(options),
                "azureopenai" => CreateAzureOpenAiGenerator(options),
                _ => throw new InvalidOperationException(
                    $"Unknown embedding provider: {options.Provider}. " +
                    "Supported: Local, Ollama, OpenAI, AzureOpenAI.")
            };
        });

        // Pre-register LocalEmbeddings if provider is Local (checked via options snapshot)
        var optionsInstance = services.BuildServiceProvider(validateScopes: false)
            .GetRequiredService<IOptions<EmbedderOptions>>().Value;
        
        if (optionsInstance.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddLocalEmbeddings(localOptions =>
            {
                localOptions.ModelName = optionsInstance.Model;
                localOptions.MaxSequenceLength = optionsInstance.MaxSequenceLength;
            });
        }

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
        // TODO: OpenAI provider implementation requires compatible M.E.AI.OpenAI version
        // Current packages don't expose AsEmbeddingGenerator extension for OpenAIClient
        // Phase 3 ships with Ollama support; OpenAI/Azure will be completed in Phase 4
        throw new NotImplementedException(
            "OpenAI provider not yet implemented. Use 'Local' or 'Ollama' provider.");
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAiGenerator(
        EmbedderOptions options)
    {
        // TODO: AzureOpenAI provider implementation requires compatible M.E.AI.OpenAI version  
        // Current packages don't expose AsEmbeddingGenerator extension for OpenAIClient
        // Phase 3 ships with Ollama support; OpenAI/Azure will be completed in Phase 4
        throw new NotImplementedException(
            "AzureOpenAI provider not yet implemented. Use 'Local' or 'Ollama' provider.");
    }
}
