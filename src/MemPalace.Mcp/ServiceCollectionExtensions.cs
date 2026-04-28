using MemPalace.Mcp.Security;
using MemPalace.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MemPalace.Mcp.Transports;

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

    /// <summary>
    /// Adds MemPalace MCP server with HTTP/SSE transport (for web-based clients).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="port">HTTP server port (default: 5050)</param>
    /// <param name="basePath">Base path for SSE endpoints (default: /mcp)</param>
    public static void AddMemPalaceMcpWithSse(this IServiceCollection services, int port = 5050, string basePath = "/mcp")
    {
        // Register HttpSseTransport as singleton
        services.AddSingleton<HttpSseTransport>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<HttpSseTransport>>();
            var sessionManager = sp.GetRequiredService<SessionManager>();
            return new HttpSseTransport(logger, sessionManager, basePath, port);
        });

        // Register SessionManager if not already registered
        services.AddSingleton<SessionManager>();

        // Note: HttpSseTransport will be started manually in the CLI command
        // We don't use the IMcpServerBuilder pattern here because SSE requires
        // different lifecycle management (web server hosting vs. stdio stream)
    }
}
