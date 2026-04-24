using System.Text.Json;
using MemPalace.Benchmarks.Core;
using MemPalace.Benchmarks.Runners;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MemPalace.Benchmarks.Commands;

internal sealed class ListCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var benchmarks = GetAllBenchmarks();

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Description");

        foreach (var bench in benchmarks)
        {
            table.AddRow(bench.Name, bench.Description);
        }

        AnsiConsole.Write(table);
        return 0;
    }

    internal static List<IBenchmark> GetAllBenchmarks()
    {
        return new List<IBenchmark>
        {
            new LongMemEvalBenchmark(),
            new LoCoMoBenchmark(),
            new ConvoMemBenchmark(),
            new MemBenchBenchmark()
        };
    }
}
