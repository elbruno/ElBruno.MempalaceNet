using MemPalace.Core.Backends;
using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;
using Azure;

namespace MemPalace.Ai.Embedding;

/// <summary>
/// Factory for creating embedders from options or custom instances.
/// </summary>
public static class EmbedderFactory
{
    /// <summary>
    /// Creates an embedder from configuration options.
    /// </summary>
    /// <param name="options">Embedder configuration (type, model, API key, etc.)</param>
    /// <returns>Configured IEmbedder instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown for invalid configuration</exception>
    public static IEmbedder Create(EmbedderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Custom embedder takes precedence
        if (options.CustomEmbedder != null)
        {
            ValidateCustomEmbedder(options.CustomEmbedder);
            return options.CustomEmbedder;
        }

        // Built-in embedder types
        return options.Type switch
        {
            EmbedderType.Local => CreateLocalEmbedder(options),
            EmbedderType.OpenAI => CreateOpenAiEmbedder(options),
            EmbedderType.AzureOpenAI => CreateAzureOpenAiEmbedder(options),
            _ => throw new InvalidOperationException(
                $"Unknown embedder type: {options.Type}. " +
                "Supported: Local, OpenAI, AzureOpenAI.")
        };
    }

    /// <summary>
    /// Creates a custom embedder instance (convenience overload).
    /// </summary>
    /// <param name="customEmbedder">Custom embedder implementation</param>
    /// <returns>Validated IEmbedder instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when customEmbedder is null</exception>
    /// <exception cref="ArgumentException">Thrown when custom embedder is invalid</exception>
    public static IEmbedder CreateCustom(ICustomEmbedder customEmbedder)
    {
        ArgumentNullException.ThrowIfNull(customEmbedder);
        ValidateCustomEmbedder(customEmbedder);
        return customEmbedder;
    }

    private static void ValidateCustomEmbedder(ICustomEmbedder embedder)
    {
        if (string.IsNullOrWhiteSpace(embedder.ModelIdentity))
        {
            throw new ArgumentException(
                "Custom embedder ModelIdentity cannot be null or empty.",
                nameof(embedder));
        }

        if (embedder.Dimensions <= 0)
        {
            throw new ArgumentException(
                $"Custom embedder Dimensions must be positive (got {embedder.Dimensions}).",
                nameof(embedder));
        }
    }

    private static IEmbedder CreateLocalEmbedder(EmbedderOptions options)
    {
        return new LocalEmbedder(
            modelName: options.Model,
            maxSequenceLength: options.MaxSequenceLength);
    }

    private static IEmbedder CreateOpenAiEmbedder(EmbedderOptions options)
    {
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is required. Set EmbedderOptions.ApiKey or environment variable OPENAI_API_KEY.");
        }

        var model = options.Model ?? "text-embedding-3-small";

        var generator = new OpenAIEmbeddingGenerator(apiKey, model);
        return new MeaiEmbedder(generator, "openai", model);
    }

    private static IEmbedder CreateAzureOpenAiEmbedder(EmbedderOptions options)
    {
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

        var generator = new AzureOpenAIEmbeddingGenerator(
            options.Endpoint,
            apiKey,
            options.DeploymentName);
        return new MeaiEmbedder(generator, "azureopenai", options.DeploymentName);
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
