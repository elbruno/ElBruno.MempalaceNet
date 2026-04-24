using System.ComponentModel;
using System.Text.Json;
using MemPalace.Benchmarks.Core;
using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;
using MemPalace.Search;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Benchmarks.Commands;

internal sealed class RunAllCommand : Command<RunAllCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
        [CommandOption("--datasets-dir")]
        [Description("Directory containing dataset files")]
        public string DatasetsDir { get; init; } = "";

        [CommandOption("--palace")]
        [Description("Path to the palace storage directory")]
        public string Palace { get; init; } = "";

        [CommandOption("--max")]
        [Description("Maximum number of dataset items to process per benchmark")]
        public int? MaxItems { get; init; }

        [CommandOption("--out")]
        [Description("Output JSON file for results")]
        public string? OutputFile { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DatasetsDir))
        {
            AnsiConsole.MarkupLine("[red]--datasets-dir is required[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Palace))
        {
            AnsiConsole.MarkupLine("[red]--palace is required[/]");
            return 1;
        }

        var benchmarks = ListCommand.GetAllBenchmarks();
        var results = new List<BenchmarkResult>();
        var services = BuildServices(settings.Palace);

        foreach (var benchmark in benchmarks)
        {
            var datasetPath = Path.Combine(settings.DatasetsDir, $"{benchmark.Name}.jsonl");
            if (!File.Exists(datasetPath))
            {
                AnsiConsole.MarkupLine($"[yellow]Dataset not found for {benchmark.Name}: {datasetPath}[/]");
                continue;
            }

            var ctx = new BenchmarkContext(datasetPath, settings.Palace, services, settings.MaxItems);

            var result = AnsiConsole.Status()
                .Start($"Running {benchmark.Name}...", _ =>
                {
                    return benchmark.RunAsync(ctx).GetAwaiter().GetResult();
                });

            results.Add(result);
            DisplayResult(result);
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputFile))
        {
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settings.OutputFile, json);
            AnsiConsole.MarkupLine($"[green]Results saved to {settings.OutputFile}[/]");
        }

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
        table.AddRow("Recall@10", $"{result.Recall:F4}");
        table.AddRow("Precision@10", $"{result.Precision:F4}");
        table.AddRow("F1", $"{result.F1:F4}");
        table.AddRow("NDCG@10", $"{result.NdcgAt10:F4}");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
