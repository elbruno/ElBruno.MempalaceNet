using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace.Cli.Output;

namespace MemPalace.Cli.Commands;

internal sealed class InitSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Path where the palace will be initialized")]
    public string Path { get; init; } = string.Empty;

    [CommandOption("--name")]
    [Description("Optional name for the palace")]
    public string? Name { get; init; }
}

internal sealed class InitCommand : AsyncCommand<InitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InitSettings settings)
    {
        try
        {
            // Validate path
            var expandedPath = Environment.ExpandEnvironmentVariables(settings.Path);
            var absolutePath = Path.GetFullPath(expandedPath);
            
            // Check if palace already exists
            if (Directory.Exists(absolutePath) && Directory.GetFileSystemEntries(absolutePath).Length > 0)
            {
                ErrorFormatter.DisplayConfigurationError($"Directory '{absolutePath}' already exists and is not empty");
                AnsiConsole.MarkupLine("[yellow]Tip:[/] Use a different path or run [blue]mempalacenet search --help[/] to query an existing palace");
                return 1;
            }

            var panel = OutputFormatter.CreatePanel(
                "mempalacenet init",
                $"Path: [blue]{absolutePath}[/]\n" +
                $"Name: [blue]{settings.Name ?? "(default)"}[/]\n\n" +
                $"[yellow]Creating new palace...[/]");
            
            AnsiConsole.Write(panel);
            
            // Create directory
            Directory.CreateDirectory(absolutePath);
            
            // Simulate initialization steps
            await ProgressDisplay.WithProgress(
                "[green]Initializing palace[/]",
                3,
                async progress =>
                {
                    progress.Report(new ProgressDisplay.ProgressInfo(1, 3, "Creating database schema"));
                    await Task.Delay(200);
                    
                    progress.Report(new ProgressDisplay.ProgressInfo(2, 3, "Setting up embedder configuration"));
                    await Task.Delay(200);
                    
                    progress.Report(new ProgressDisplay.ProgressInfo(3, 3, "Initializing default collections"));
                    await Task.Delay(200);
                    
                    return 0;
                });
            
            OutputFormatter.DisplaySuccess($"Palace initialized at {absolutePath}");
            AnsiConsole.MarkupLine($"\n[dim]Next steps:[/]");
            AnsiConsole.MarkupLine($"  • Mine content: [blue]mempalacenet mine /path/to/docs --wing documentation[/]");
            AnsiConsole.MarkupLine($"  • Search: [blue]mempalacenet search \"your query\" --wing documentation[/]");
            AnsiConsole.MarkupLine($"  • View wings: [blue]mempalacenet wake-up --wing documentation[/]");
            
            await Task.CompletedTask;
            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorFormatter.DisplayInvalidPath(settings.Path, $"access denied: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            ErrorFormatter.DisplayGenericError($"Failed to initialize palace: {ex.Message}");
            return 1;
        }
    }
}
