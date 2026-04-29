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
    /// This now uses BM25 for keyword search (upgraded from simple token overlap in v0.5).
    /// </summary>
    public static IServiceCollection AddHybridSearch(this IServiceCollection services)
    {
        return services.AddEnhancedHybridSearch();
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
            return new Bm25SearchService(backend);
        });
        
        return services;
    }

    /// <summary>
    /// Registers an enhanced HybridSearchService that combines vector search with BM25 keyword search.
    /// Provides the best of both worlds: semantic + keyword relevance with optional reranking.
    /// </summary>
    public static IServiceCollection AddEnhancedHybridSearch(this IServiceCollection services)
    {
        services.AddSingleton<Bm25SearchService>(sp =>
        {
            var backend = sp.GetRequiredService<IBackend>();
            return new Bm25SearchService(backend);
        });

        services.AddSingleton<ISearchService>(sp =>
        {
            var backend = sp.GetRequiredService<IBackend>();
            var embedder = sp.GetRequiredService<IEmbedder>();
            var bm25Service = sp.GetRequiredService<Bm25SearchService>();
            var reranker = sp.GetService<IReranker>();
            return new HybridSearchService(backend, embedder, bm25Service, reranker);
        });
        
        return services;
    }
}
