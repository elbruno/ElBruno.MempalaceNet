using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MemPalace.KnowledgeGraph;

/// <summary>
/// DI registration extensions for knowledge graph.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the knowledge graph services.
    /// </summary>
    public static IServiceCollection AddMemPalaceKnowledgeGraph(
        this IServiceCollection services,
        Action<KnowledgeGraphOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton<IKnowledgeGraph>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KnowledgeGraphOptions>>().Value;
            
            if (string.IsNullOrEmpty(options.DatabasePath))
            {
                throw new InvalidOperationException("DatabasePath must be configured in KnowledgeGraphOptions.");
            }

            var directory = Path.GetDirectoryName(options.DatabasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return new SqliteKnowledgeGraph(options.DatabasePath);
        });

        return services;
    }
}
