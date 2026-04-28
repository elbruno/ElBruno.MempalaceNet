using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace.Cli.Output;

namespace MemPalace.Cli.Commands;

internal sealed class MineSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Path to mine for memories")]
    public string Path { get; init; } = string.Empty;

    [CommandOption("--mode")]
    [Description("Mining mode: files or convos")]
    [DefaultValue("files")]
    public string Mode { get; init; } = "files";

    [CommandOption("--wing")]
    [Description("Target wing for mined content")]
    public string? Wing { get; init; }
    
    [CommandOption("--collection")]
    [Description("Collection name")]
    [DefaultValue("memories")]
    public string Collection { get; init; } = "memories";
    
    [CommandOption("--verbose")]
    [Description("Enable verbose output")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}

internal sealed class MineCommand : AsyncCommand<MineSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, MineSettings settings)
    {
        try
        {
            // Validate path
            if (!Directory.Exists(settings.Path))
            {
                ErrorFormatter.DisplayInvalidPath(settings.Path, "directory does not exist");
                return 1;
            }

            var panel = OutputFormatter.CreatePanel(
                "mempalacenet mine",
                $"Path: [blue]{settings.Path}[/]\n" +
                $"Mode: [blue]{settings.Mode}[/]\n" +
                $"Wing: [blue]{settings.Wing ?? "(auto-detect)"}[/]\n" +
                $"Collection: [blue]{settings.Collection}[/]\n" +
                $"Verbose: [blue]{settings.Verbose}[/]");
            
            AnsiConsole.Write(panel);
            
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine("[dim]Verbose mode enabled - detailed logs will be shown[/]");
            }
            
            // Simulate mining with progress tracking
            var files = Directory.GetFiles(settings.Path, "*.*", SearchOption.AllDirectories)
                .Take(50) // Limit for demo
                .ToArray();
            
            if (files.Length == 0)
            {
                ErrorFormatter.DisplayMiningError(settings.Path, "no files found to mine");
                return 1;
            }

            await ProgressDisplay.WithMiningProgress(
                "[green]Mining memories[/]",
                files.Length,
                async progress =>
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        var relativePath = Path.GetRelativePath(settings.Path, file);
                        
                        if (settings.Verbose)
                        {
                            AnsiConsole.MarkupLine($"[dim]Processing: {relativePath}[/]");
                        }
                        
                        progress.Report(new ProgressDisplay.MiningProgress(
                            ProcessedFiles: i + 1,
                            TotalFiles: files.Length,
                            CurrentFile: relativePath));
                        
                        await Task.Delay(20); // Simulate processing
                    }
                    
                    return files.Length;
                });
            
            OutputFormatter.DisplaySuccess($"Successfully mined {files.Length} files");
            AnsiConsole.MarkupLine($"[dim]Memories stored in wing: {settings.Wing ?? "auto-detected"}[/]");
            
            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorFormatter.DisplayMiningError(settings.Path, $"access denied: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            if (settings.Verbose)
            {
                ErrorFormatter.DisplayGenericError(ex.Message, ex.StackTrace ?? "");
            }
            else
            {
                ErrorFormatter.DisplayMiningError(settings.Path, ex.Message);
            }
            return 1;
        }
    }
}
