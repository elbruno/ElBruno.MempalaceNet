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

        // Register IEmbedder via factory
        services.AddSingleton<IEmbedder>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbedderOptions>>().Value;
            return EmbedderFactory.Create(options);
        });

        return services;
    }
}
