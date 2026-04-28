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

        if (transport != "stdio")
        {
            await Console.Error.WriteLineAsync($"Error: Transport '{settings.Transport}' is not yet supported.");
            await Console.Error.WriteLineAsync("SSE transport is planned for a future release.");
            await Console.Error.WriteLineAsync("Currently available: stdio");
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

        // Register the MCP server with stdio transport
        builder.Services.AddMemPalaceMcpWithStdio();
        await Console.Error.WriteLineAsync("[INFO] Starting MCP server with stdio transport");

        // Build and run the host
        var host = builder.Build();
        await host.RunAsync();

        return 0;
    }
}
