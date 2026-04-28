using Spectre.Console;

namespace MemPalace.Cli.Output;

/// <summary>
/// Formats error messages with contextual information and remediation steps.
/// </summary>
internal static class ErrorFormatter
{
    public static void DisplayError(string errorType, string message, params string[] remediationSteps)
    {
        var panel = new Panel(BuildErrorContent(message, remediationSteps))
        {
            Header = new PanelHeader($"[red bold]Error: {errorType}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red)
        };
        
        AnsiConsole.Write(panel);
    }

    public static void DisplayPalaceNotFound(string path)
    {
        DisplayError(
            "Palace Not Found",
            $"No palace found at path: [blue]{path}[/]",
            $"Initialize a palace: [yellow]mempalacenet init {path}[/]",
            "Or specify an existing palace path with [yellow]--path[/] flag",
            "Check file permissions on the specified directory"
        );
    }

    public static void DisplayInvalidPath(string path, string reason)
    {
        DisplayError(
            "Invalid Path",
            $"Path '[blue]{path}[/]' is invalid: {reason}",
            "Ensure the path exists and is accessible",
            "Check for typos in the path",
            "Verify file permissions"
        );
    }

    public static void DisplayMiningError(string path, string error)
    {
        DisplayError(
            "Mining Failed",
            $"Failed to mine path '[blue]{path}[/]': {error}",
            "Verify the path exists and contains readable files",
            "Check file permissions",
            "Try with [yellow]--verbose[/] flag for detailed logs"
        );
    }

    public static void DisplaySearchError(string query, string error)
    {
        DisplayError(
            "Search Failed",
            $"Failed to execute search for '[blue]{query}[/]': {error}",
            "Ensure the palace is initialized and contains memories",
            "Check that the specified wing exists",
            "Verify embedder configuration with [yellow]mempalacenet health[/]"
        );
    }

    public static void DisplayEmbedderError(string error)
    {
        DisplayError(
            "Embedder Error",
            $"Embedder operation failed: {error}",
            "Check embedder configuration in palace settings",
            "For local ONNX: ensure model files are present",
            "For API embedders: verify API keys and connectivity",
            "Run [yellow]mempalacenet health[/] to diagnose"
        );
    }

    public static void DisplayBackendError(string error)
    {
        DisplayError(
            "Backend Error",
            $"Database operation failed: {error}",
            "Check database file permissions",
            "Verify the palace path is correct",
            "Try reinitializing the palace if corruption is suspected"
        );
    }

    public static void DisplayConfigurationError(string error)
    {
        DisplayError(
            "Configuration Error",
            $"Configuration is invalid: {error}",
            "Check palace configuration file syntax",
            "Verify all required settings are present",
            "See [yellow]mempalacenet init --help[/] for valid options"
        );
    }

    public static void DisplayGenericError(string error, string context = "")
    {
        var message = string.IsNullOrEmpty(context) 
            ? $"{error}" 
            : $"{error}\n\nContext: [dim]{context}[/]";
            
        DisplayError(
            "Operation Failed",
            message,
            "Check the command syntax with [yellow]--help[/]",
            "Try with [yellow]--verbose[/] flag for more details",
            "Report persistent issues at: https://github.com/elbruno/mempalacenet/issues"
        );
    }

    private static string BuildErrorContent(string message, string[] remediationSteps)
    {
        var content = $"{message}\n\n";
        
        if (remediationSteps.Length > 0)
        {
            content += "[yellow bold]Remediation steps:[/]\n";
            for (int i = 0; i < remediationSteps.Length; i++)
            {
                content += $"  [yellow]{i + 1}.[/] {remediationSteps[i]}\n";
            }
            content += "\n[dim]For more help, run the command with --help flag[/]";
        }
        
        return content;
    }
}
