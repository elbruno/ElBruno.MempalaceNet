using MemPalace.Cli.Commands;
using MemPalace.Cli.Commands.Agents;
using MemPalace.Cli.Commands.Kg;
using MemPalace.Cli.Infrastructure;
using MemPalace.KnowledgeGraph;
using MemPalace.Mining;
using MemPalace.Search;
using MemPalace.Agents;
using MemPalace.Core.Services;
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
        
        // Register mining and search
        services.AddMemPalaceMining();
        services.AddMemPalaceSearch();
        
        // Register Knowledge Graph
        var palaceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemPalace");
        services.AddMemPalaceKnowledgeGraph(o => 
            o.DatabasePath = Path.Combine(palaceDir, "mempalace-kg.db"));
        
        // Register IChatClient if configured
        // Users must register an IChatClient for agents to work (e.g., via AddChatClient or AddOpenAIChatClient)
        // Example: services.AddOpenAIChatClient("model", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        // Without IChatClient, agent commands will fail with a clear error message.
        
        // Register Agents
        services.AddMemPalaceAgents(o =>
            o.AgentsPath = Path.Combine(Directory.GetCurrentDirectory(), ".mempalace", "agents"));
        
        // Register WakeUp service
        services.AddSingleton<IWakeUpService>(sp =>
        {
            var chatClient = sp.GetService<Microsoft.Extensions.AI.IChatClient>();
            return new WakeUpService(chatClient);
        });
        
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

            config.AddCommand<McpCommand>("mcp")
                .WithDescription("Start MCP server (Model Context Protocol)")
                .WithExample("mempalacenet mcp")
                .WithExample("mempalacenet mcp --transport stdio");

            // Agents branch
            config.AddBranch("agents", agents =>
            {
                agents.SetDescription("Agent management commands");
                
                agents.AddCommand<AgentsListCommand>("list")
                    .WithDescription("List all agents in the palace")
                    .WithExample("mempalacenet agents list");
                
                agents.AddCommand<AgentsRunCommand>("run")
                    .WithDescription("Run an agent with a one-shot message")
                    .WithExample("mempalacenet agents run scribe \"What is MemPalace?\"");
                
                agents.AddCommand<AgentsChatCommand>("chat")
                    .WithDescription("Start an interactive chat with an agent")
                    .WithExample("mempalacenet agents chat scribe");
                
                agents.AddCommand<AgentsDiaryCommand>("diary")
                    .WithDescription("View or search an agent's diary")
                    .WithExample("mempalacenet agents diary scribe --tail 10")
                    .WithExample("mempalacenet agents diary scribe --search \"knowledge graph\"");
            });

            // Knowledge graph branch
            config.AddBranch("kg", kg =>
            {
                kg.SetDescription("Knowledge graph operations");
                
                kg.AddCommand<KgAddCommand>("add")
                    .WithDescription("Add a relationship to the knowledge graph")
                    .WithExample("mempalacenet kg add agent:Tyrell worked-on project:MemPalace.Core")
                    .WithExample("mempalacenet kg add agent:Tyrell worked-on phase:Phase1 --valid-from 2026-04-24T10:00:00");

                kg.AddCommand<KgQueryCommand>("query")
                    .WithDescription("Query the knowledge graph")
                    .WithExample("mempalacenet kg query \"? worked-on project:MemPalace.Core\"")
                    .WithExample("mempalacenet kg query \"agent:Tyrell worked-on ?\" --at 2026-04-24");

                kg.AddCommand<KgTimelineCommand>("timeline")
                    .WithDescription("View entity timeline")
                    .WithExample("mempalacenet kg timeline agent:Tyrell")
                    .WithExample("mempalacenet kg timeline project:MemPalace.Core --from 2026-04-24");
            });
        });

        return app.Run(args);
    }
}
