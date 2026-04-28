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
        var transport = settings.Transport.ToLowerInvariant();

        // Build a host for the MCP server
        var builder = Host.CreateApplicationBuilder();
        
        // Configure logging to stderr (MCP protocol uses stdout for messages)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Copy core services from CLI's DI container
        // In production, we'd use a proper service registration pattern
        // For now, we'll register the MCP server with the appropriate transport
        
        switch (transport)
        {
            case "stdio":
                builder.Services.AddMemPalaceMcpWithStdio();
                await Console.Error.WriteLineAsync("[INFO] Starting MCP server with stdio transport");
                break;

            case "sse":
                builder.Services.AddMemPalaceMcpWithSse(settings.Port);
                await Console.Error.WriteLineAsync($"[INFO] Starting MCP server with SSE transport on port {settings.Port}");
                await Console.Error.WriteLineAsync($"[INFO] Connect to http://localhost:{settings.Port}/sse");
                break;

            default:
                await Console.Error.WriteLineAsync($"Error: Transport '{settings.Transport}' is not supported. Available: stdio, sse");
                return 1;
        }

        // Build and run the host
        var host = builder.Build();
        await host.RunAsync();

        return 0;
    }
}
