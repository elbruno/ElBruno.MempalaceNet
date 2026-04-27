using System.ComponentModel;
using MemPalace.Mcp;
using MemPalace.Mcp.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Commands;

internal sealed class McpSettings : CommandSettings
{
    [CommandOption("--transport")]
    [Description("Transport type: stdio (default), sse, or both")]
    [DefaultValue("stdio")]
    public string Transport { get; init; } = "stdio";

    [CommandOption("--port")]
    [Description("Port for SSE transport (default: 5050)")]
    [DefaultValue(5050)]
    public int Port { get; init; } = 5050;

    [CommandOption("--host")]
    [Description("Host for SSE transport (default: 127.0.0.1)")]
    [DefaultValue("127.0.0.1")]
    public string Host { get; init; } = "127.0.0.1";
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

        // Validate transport option
        if (transport != "stdio" && transport != "sse" && transport != "both")
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Invalid transport type '{0}'", settings.Transport);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Available transports:[/]");
            AnsiConsole.MarkupLine("  • [green]stdio[/] - Standard input/output (default, local only)");
            AnsiConsole.MarkupLine("  • [green]sse[/]   - HTTP/SSE (Server-Sent Events, web clients, Copilot CLI)");
            AnsiConsole.MarkupLine("  • [green]both[/]  - Run stdio and SSE simultaneously (experimental)");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Examples:[/]");
            AnsiConsole.MarkupLine("  mempalacenet mcp");
            AnsiConsole.MarkupLine("  mempalacenet mcp --transport sse --port 5050");
            AnsiConsole.MarkupLine("  mempalacenet mcp --transport both --port 5050");
            return 1;
        }

        var runStdio = transport == "stdio" || transport == "both";
        var runSse = transport == "sse" || transport == "both";

        // Build a host for the MCP server
        var builder = Host.CreateApplicationBuilder();
        
        // Configure logging to stderr (MCP protocol uses stdout for messages)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Register core MCP services
        if (runStdio)
        {
            builder.Services.AddMemPalaceMcpWithStdio();
        }

        if (runSse)
        {
            builder.Services.AddMemPalaceMcpWithSse(settings.Port, "/mcp");
        }

        // Build the host
        var host = builder.Build();

        // Start SSE transport if enabled
        if (runSse)
        {
            var sseTransport = host.Services.GetRequiredService<HttpSseTransport>();
            await sseTransport.StartAsync();
            
            AnsiConsole.MarkupLine("[green]✓[/] HTTP/SSE transport started on [cyan]http://{0}:{1}/mcp[/]", settings.Host, settings.Port);
            AnsiConsole.MarkupLine("[dim]Clients can connect to this endpoint for MCP operations[/]");
            AnsiConsole.WriteLine();
        }

        if (runStdio)
        {
            AnsiConsole.MarkupLine("[green]✓[/] stdio transport ready (listening on stdin/stdout)");
            if (runSse)
            {
                AnsiConsole.MarkupLine("[yellow]Note:[/] Running both transports. stdio will handle stdin/stdout, SSE will handle HTTP clients.");
            }
            AnsiConsole.WriteLine();
        }

        // Run the host
        await host.RunAsync();

        return 0;
    }
}
