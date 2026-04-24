using BenchmarkDotNet.Running;
using Spectre.Console.Cli;

namespace MemPalace.Benchmarks.Commands;

internal sealed class MicroCommand : Command
{
    public override int Execute(CommandContext context)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly);
        return 0;
    }
}
