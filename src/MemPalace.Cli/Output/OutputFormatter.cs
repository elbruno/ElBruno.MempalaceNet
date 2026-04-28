using Spectre.Console;

namespace MemPalace.Cli.Output;

/// <summary>
/// Utilities for formatting CLI output (tables, panels, etc.)
/// </summary>
internal static class OutputFormatter
{
    /// <summary>
    /// Creates a styled panel for command output.
    /// </summary>
    public static Panel CreatePanel(string title, string content, Color borderColor = default)
    {
        var color = borderColor == default ? Color.Green : borderColor;
        
        return new Panel(content)
        {
            Header = new PanelHeader($"[bold]{title}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(color)
        };
    }

    /// <summary>
    /// Creates a success message panel.
    /// </summary>
    public static void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    /// <summary>
    /// Creates a warning message panel.
    /// </summary>
    public static void DisplayWarning(string message)
    {
        var panel = new Panel($"[yellow]{message}[/]")
        {
            Header = new PanelHeader("[yellow bold]Warning[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };
        
        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Creates a table for search results.
    /// </summary>
    public static Table CreateSearchResultsTable()
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn(new TableColumn("[bold]Score[/]").RightAligned());
        table.AddColumn("[bold]Wing[/]");
        table.AddColumn("[bold]Room[/]");
        table.AddColumn("[bold]Content[/]");
        table.AddColumn(new TableColumn("[bold]Timestamp[/]").RightAligned());
        return table;
    }

    /// <summary>
    /// Creates a table for knowledge graph results.
    /// </summary>
    public static Table CreateKnowledgeGraphTable()
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("[bold]From[/]");
        table.AddColumn("[bold]Relationship[/]");
        table.AddColumn("[bold]To[/]");
        table.AddColumn("[bold]Valid From[/]");
        table.AddColumn("[bold]Valid To[/]");
        return table;
    }

    /// <summary>
    /// Creates a table for agent listings.
    /// </summary>
    public static Table CreateAgentsTable()
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("[bold]Agent ID[/]");
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Wing[/]");
        table.AddColumn("[bold]Status[/]");
        return table;
    }

    /// <summary>
    /// Creates a table for wings/collections.
    /// </summary>
    public static Table CreateCollectionsTable()
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("[bold]Collection[/]");
        table.AddColumn("[bold]Wing[/]");
        table.AddColumn(new TableColumn("[bold]Memories[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
        return table;
    }

    /// <summary>
    /// Formats a file size for display.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Truncates text to a maximum length with ellipsis.
    /// </summary>
    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
            
        return text[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Formats a timestamp for display.
    /// </summary>
    public static string FormatTimestamp(DateTime timestamp)
    {
        var now = DateTime.UtcNow;
        var diff = now - timestamp;
        
        return diff.TotalDays switch
        {
            < 1 => diff.TotalHours < 1 
                ? $"{(int)diff.TotalMinutes}m ago"
                : $"{(int)diff.TotalHours}h ago",
            < 7 => $"{(int)diff.TotalDays}d ago",
            < 30 => $"{(int)(diff.TotalDays / 7)}w ago",
            _ => timestamp.ToString("yyyy-MM-dd")
        };
    }
}
