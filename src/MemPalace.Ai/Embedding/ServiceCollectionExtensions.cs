using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ElBruno.LocalEmbeddings.Extensions;
using OpenAI;
using Azure.AI.OpenAI;
using Azure;

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

        // Pre-register LocalEmbeddings if provider is Local (checked via options snapshot)
        var optionsInstance = services.BuildServiceProvider(validateScopes: false)
            .GetRequiredService<IOptions<EmbedderOptions>>().Value;
        
        if (optionsInstance.Type == EmbedderType.Local)
        {
            services.AddLocalEmbeddings(localOptions =>
            {
                localOptions.ModelName = optionsInstance.Model;
                localOptions.MaxSequenceLength = optionsInstance.MaxSequenceLength;
            });
        }

        // Register IEmbeddingGenerator factory based on embedder type
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;

            return options.Type switch
            {
                EmbedderType.Local => sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                EmbedderType.OpenAI => CreateOpenAiGenerator(options),
                EmbedderType.AzureOpenAI => CreateAzureOpenAiGenerator(options),
                _ => throw new InvalidOperationException(
                    $"Unknown embedding provider type: {options.Type}. " +
                    "Supported: Local, OpenAI, AzureOpenAI.")
            };
        });

        // Register the IEmbedder adapter
        services.AddSingleton<IEmbedder>(sp =>
        {
            var generator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;
            var providerName = options.Type.ToString().ToLowerInvariant();
            return new MeaiEmbedder(generator, providerName, options.Model);
        });

        return services;
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAiGenerator(
        EmbedderOptions options)
    {
        // Validate API key
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is required. Set EmbedderOptions.ApiKey or environment variable OPENAI_API_KEY.");
        }

        var model = options.Model ?? "text-embedding-3-small";
        
        // Create a simple wrapper for OpenAI embeddings
        return new OpenAIEmbeddingGenerator(apiKey, model);
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAiGenerator(
        EmbedderOptions options)
    {
        // Validate required parameters
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI API key is required. Set EmbedderOptions.ApiKey or environment variable AZURE_OPENAI_API_KEY.");
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException(
                "Azure OpenAI endpoint is required. Set EmbedderOptions.Endpoint.");
        }

        if (string.IsNullOrWhiteSpace(options.DeploymentName))
        {
            throw new InvalidOperationException(
                "Azure OpenAI deployment name is required. Set EmbedderOptions.DeploymentName.");
        }

        // Create a simple wrapper for Azure OpenAI embeddings
        return new AzureOpenAIEmbeddingGenerator(
            options.Endpoint, 
            apiKey, 
            options.DeploymentName);
    }

    // Simple wrapper for OpenAI embeddings
    private sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly OpenAI.OpenAIClient _client;
        private readonly string _model;

        public OpenAIEmbeddingGenerator(string apiKey, string model)
        {
            _client = new OpenAI.OpenAIClient(apiKey);
            _model = model;
        }

        public EmbeddingGeneratorMetadata Metadata => new("openai");

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, 
            EmbeddingGenerationOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            var embedOptions = new OpenAI.Embeddings.EmbeddingGenerationOptions
            {
                Dimensions = options?.Dimensions
            };

            var response = await _client.GetEmbeddingClient(_model)
                .GenerateEmbeddingsAsync(values, embedOptions, cancellationToken);

            var embeddings = response.Value
                .Select(e => new Embedding<float>(e.ToFloats().ToArray()))
                .ToList();

            return new GeneratedEmbeddings<Embedding<float>>(embeddings);
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        
        public void Dispose() { }
    }

    // Simple wrapper for Azure OpenAI embeddings
    private sealed class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly Azure.AI.OpenAI.AzureOpenAIClient _client;
        private readonly string _deploymentName;

        public AzureOpenAIEmbeddingGenerator(string endpoint, string apiKey, string deploymentName)
        {
            _client = new Azure.AI.OpenAI.AzureOpenAIClient(
                new Uri(endpoint),
                new Azure.AzureKeyCredential(apiKey));
            _deploymentName = deploymentName;
        }

        public EmbeddingGeneratorMetadata Metadata => new("azureopenai");

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, 
            EmbeddingGenerationOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            var embedOptions = new OpenAI.Embeddings.EmbeddingGenerationOptions
            {
                Dimensions = options?.Dimensions
            };

            var response = await _client.GetEmbeddingClient(_deploymentName)
                .GenerateEmbeddingsAsync(values, embedOptions, cancellationToken);

            var embeddings = response.Value
                .Select(e => new Embedding<float>(e.ToFloats().ToArray()))
                .ToList();

            return new GeneratedEmbeddings<Embedding<float>>(embeddings);
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        
        public void Dispose() { }
    }
}
