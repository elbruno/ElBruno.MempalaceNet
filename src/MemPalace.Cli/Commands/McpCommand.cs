using System.ComponentModel;
using MemPalace.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class McpSettings : CommandSettings
{
    [CommandOption("--transport")]
    [Description("Transport type: stdio (default) or sse")]
    [DefaultValue("stdio")]
    public string Transport { get; init; } = "stdio";

    [CommandOption("--port")]
    [Description("Port for SSE transport (default: 5050)")]
    [DefaultValue(5050)]
    public int Port { get; init; } = 5050;
}

internal sealed class McpCommand : AsyncCommand<McpSettings>
{
    private readonly IServiceProvider _serviceProvider;

    public McpCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, McpSettings settings)
    {
        if (settings.Transport.ToLowerInvariant() != "stdio")
        {
            // For now, only stdio is supported
            // SSE would require MemPalace.Mcp.AspNetCore package
            await Console.Error.WriteLineAsync($"Error: Transport '{settings.Transport}' is not yet supported. Only 'stdio' is currently available.");
            return 1;
        }

        // Build a host for the MCP server
        var builder = Host.CreateApplicationBuilder();
        
        // Configure logging to stderr (MCP protocol uses stdout for messages)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Copy services from CLI's DI container to the host builder
        // This ensures the MCP server has access to the same backend, search, KG instances
        var cliServices = _serviceProvider;
        
        // Re-register core services in the host
        // Note: In a real implementation, we'd need to pass configuration and properly
        // register all services. For now, we'll register the MCP server which will
        // expect injected services to be available.
        builder.Services.AddMemPalaceMcpWithStdio();

        // Build and run the host
        var host = builder.Build();
        await host.RunAsync();

        return 0;
    }
}
