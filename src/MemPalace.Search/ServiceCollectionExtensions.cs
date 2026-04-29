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

    /// <summary>
    /// Registers BM25SearchService as the ISearchService.
    /// Provides keyword-only search using the BM25 algorithm.
    /// </summary>
    public static IServiceCollection AddBM25Search(this IServiceCollection services)
    {
        services.AddSingleton<ISearchService>(sp =>
        {
            var backend = sp.GetRequiredService<IBackend>();
            var embedder = sp.GetRequiredService<IEmbedder>();
            return new BM25SearchService(backend, embedder);
        });
        
        return services;
    }
}
