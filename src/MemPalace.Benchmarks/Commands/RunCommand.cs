using System.ComponentModel;
using MemPalace.Benchmarks.Core;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Search;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Benchmarks.Commands;

internal sealed class RunCommand : Command<RunCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("Benchmark name (use 'list' to see available benchmarks)")]
        public string Name { get; init; } = "";

        [CommandOption("--dataset")]
        [Description("Path to the dataset file (JSONL)")]
        public string Dataset { get; init; } = "";

        [CommandOption("--palace")]
        [Description("Path to the palace storage directory")]
        public string Palace { get; init; } = "";

        [CommandOption("--max")]
        [Description("Maximum number of dataset items to process")]
        public int? MaxItems { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Benchmark name is required[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Dataset))
        {
            AnsiConsole.MarkupLine("[red]--dataset is required[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Palace))
        {
            AnsiConsole.MarkupLine("[red]--palace is required[/]");
            return 1;
        }

        var benchmark = ListCommand.GetAllBenchmarks()
            .FirstOrDefault(b => b.Name.Equals(settings.Name, StringComparison.OrdinalIgnoreCase));

        if (benchmark == null)
        {
            AnsiConsole.MarkupLine($"[red]Benchmark '{settings.Name}' not found. Use 'list' to see available benchmarks.[/]");
            return 1;
        }

        var services = BuildServices(settings.Palace);
        var ctx = new BenchmarkContext(settings.Dataset, settings.Palace, services, settings.MaxItems);

        var result = AnsiConsole.Status()
            .Start($"Running {benchmark.Name}...", _ =>
            {
                return benchmark.RunAsync(ctx).GetAwaiter().GetResult();
            });

        DisplayResult(result);
        return 0;
    }

    private static IServiceProvider BuildServices(string palacePath)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmbedder>(new DeterministicEmbedder(384));
        services.AddSingleton<IBackend>(sp => new SqliteBackend(palacePath));
        services.AddSingleton<ISearchService, VectorSearchService>();
        return services.BuildServiceProvider();
    }

    private static void DisplayResult(BenchmarkResult result)
    {
        var table = new Table();
        table.Title = new TableTitle($"[bold]{result.BenchmarkName}[/]");
        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Total Queries", result.TotalQueries.ToString());
        table.AddRow("Correct", result.Correct.ToString());
        table.AddRow("Recall@10", $"{result.Recall:F4}");
        table.AddRow("Precision@10", $"{result.Precision:F4}");
        table.AddRow("F1", $"{result.F1:F4}");
        table.AddRow("NDCG@10", $"{result.NdcgAt10:F4}");
        table.AddRow("Duration", result.TotalDuration.ToString());

        foreach (var (key, value) in result.ExtraMetrics)
        {
            table.AddRow(key, $"{value:F4}");
        }

        AnsiConsole.Write(table);
    }
}
