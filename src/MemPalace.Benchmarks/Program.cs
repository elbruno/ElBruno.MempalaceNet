using MemPalace.Benchmarks.Commands;
using Spectre.Console.Cli;

namespace MemPalace.Benchmarks;

internal sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        
        app.Configure(config =>
        {
            config.SetApplicationName("mempalacenet-bench");
            
            config.AddCommand<ListCommand>("list")
                .WithDescription("List available benchmarks");
            
            config.AddCommand<RunCommand>("run")
                .WithDescription("Run a single benchmark");
            
            config.AddCommand<RunAllCommand>("run-all")
                .WithDescription("Run all benchmarks");
            
            config.AddCommand<MicroCommand>("micro")
                .WithDescription("Run micro-benchmarks (BenchmarkDotNet)");
        });

        return app.Run(args);
    }
}
