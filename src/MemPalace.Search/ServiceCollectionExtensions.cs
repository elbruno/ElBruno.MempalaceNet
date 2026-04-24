using Microsoft.Extensions.DependencyInjection;
using MemPalace.Core.Backends;
using MemPalace.Ai.Rerank;

namespace MemPalace.Search;

/// <summary>
/// DI registration extensions for MemPalace.Search.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers VectorSearchService as the default ISearchService.
    /// </summary>
    public static IServiceCollection AddMemPalaceSearch(this IServiceCollection services)
    {
        services.AddSingleton<ISearchService>(sp =>
        {
            var backend = sp.GetRequiredService<IBackend>();
            var embedder = sp.GetRequiredService<IEmbedder>();
            var reranker = sp.GetService<IReranker>();
            return new VectorSearchService(backend, embedder, reranker);
        });
        
        return services;
    }

    /// <summary>
    /// Registers HybridSearchService as the ISearchService.
    /// </summary>
    public static IServiceCollection AddHybridSearch(this IServiceCollection services)
    {
        services.AddSingleton<ISearchService>(sp =>
        {
            var backend = sp.GetRequiredService<IBackend>();
            var embedder = sp.GetRequiredService<IEmbedder>();
            return new HybridSearchService(backend, embedder);
        });
        
        return services;
    }
}
