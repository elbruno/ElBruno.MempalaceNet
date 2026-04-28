using MemPalace.Mcp.Security;
using MemPalace.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace MemPalace.Mcp;

/// <summary>
/// Extension methods for registering the MemPalace MCP server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MemPalace MCP server services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The MCP server builder for further configuration</returns>
    public static IMcpServerBuilder AddMemPalaceMcp(this IServiceCollection services)
    {
        // Register security services
        services.AddSingleton<IAuditLogger, FileAuditLogger>();
        services.AddSingleton<SecurityValidator>();
        services.AddSingleton<IConfirmationPrompt, DefaultConfirmationPrompt>();

        // Register the tools types
        services.AddSingleton<MemPalaceMcpTools>();
        services.AddSingleton<WriteTools>();
        services.AddSingleton<KnowledgeGraphWriteTools>();

        // Register MCP server
        return services
            .AddMcpServer()
            .WithToolsFromAssembly();
    }

    /// <summary>
    /// Adds MemPalace MCP server with stdio transport (for CLI usage).
    /// </summary>
    public static IMcpServerBuilder AddMemPalaceMcpWithStdio(this IServiceCollection services)
    {
        return services
            .AddMemPalaceMcp()
            .WithStdioServerTransport();
    }
}
