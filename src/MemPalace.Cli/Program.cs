using MemPalace.Cli.Commands;
using MemPalace.Cli.Commands.Kg;
using MemPalace.Cli.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MemPalace.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("mempalace.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "MEMPALACE_")
            .Build();

        // Build service collection
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // TODO(phase2): Register SQLite backend once available
        // services.AddMemPalaceSqliteBackend();
        
        // TODO(phase3): Register local embedder (default)
        // services.AddMemPalaceAi(); // Uses Local provider by default
        
        // Register placeholder services for now so build is green
        // These will be replaced by actual registrations from other phases
        
        // Create command app with DI
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("mempalacenet");

            // Root commands
            config.AddCommand<InitCommand>("init")
                .WithDescription("Initialize a new palace")
                .WithExample("mempalacenet init ./my-palace")
                .WithExample("mempalacenet init ./my-palace --name \"My Palace\"");

            config.AddCommand<MineCommand>("mine")
                .WithDescription("Mine memories from files or conversations")
                .WithExample("mempalacenet mine ./project --mode files")
                .WithExample("mempalacenet mine ~/.claude/projects --mode convos --wing conversations");

            config.AddCommand<SearchCommand>("search")
                .WithDescription("Search for memories")
                .WithExample("mempalacenet search \"vector databases\"")
                .WithExample("mempalacenet search \"CLI design\" --wing code --rerank --top-k 5");

            config.AddCommand<WakeUpCommand>("wake-up")
                .WithDescription("Load context summary for new session")
                .WithExample("mempalacenet wake-up");

            // Agents branch
            config.AddBranch("agents", agents =>
            {
                agents.SetDescription("Agent management commands");
                
                agents.AddCommand<AgentsListCommand>("list")
                    .WithDescription("List all agents in the palace")
                    .WithExample("mempalacenet agents list");
            });

            // Knowledge graph branch
            config.AddBranch("kg", kg =>
            {
                kg.SetDescription("Knowledge graph operations");
                
                kg.AddCommand<KgAddCommand>("add")
                    .WithDescription("Add a relationship to the knowledge graph")
                    .WithExample("mempalacenet kg add Tyrell worked-on MemPalace.Core")
                    .WithExample("mempalacenet kg add Tyrell worked-on Phase1 --valid-from 2026-04-24T10:00:00");

                kg.AddCommand<KgQueryCommand>("query")
                    .WithDescription("Query the knowledge graph")
                    .WithExample("mempalacenet kg query \"? worked-on MemPalace.Core\"")
                    .WithExample("mempalacenet kg query \"Tyrell worked-on ?\" --at 2026-04-24");

                kg.AddCommand<KgTimelineCommand>("timeline")
                    .WithDescription("View entity timeline")
                    .WithExample("mempalacenet kg timeline Tyrell")
                    .WithExample("mempalacenet kg timeline MemPalace.Core --from 2026-04-24");
            });
        });

        return app.Run(args);
    }
}
