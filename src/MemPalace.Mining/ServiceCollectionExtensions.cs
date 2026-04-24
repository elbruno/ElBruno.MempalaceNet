using Microsoft.Extensions.DependencyInjection;

namespace MemPalace.Mining;

/// <summary>
/// DI registration extensions for MemPalace.Mining.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers mining services (FileSystemMiner, ConversationMiner, MiningPipeline).
    /// </summary>
    public static IServiceCollection AddMemPalaceMining(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IMiner, FileSystemMiner>("filesystem");
        services.AddKeyedSingleton<IMiner, ConversationMiner>("conversation");
        services.AddSingleton<MiningPipeline>();
        
        return services;
    }
}
