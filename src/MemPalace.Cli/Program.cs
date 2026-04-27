using MemPalace.Cli.Commands;
using MemPalace.Cli.Commands.Agents;
using MemPalace.Cli.Commands.Kg;
using MemPalace.Cli.Commands.Skill;
using MemPalace.Cli.Infrastructure;
using MemPalace.KnowledgeGraph;
using MemPalace.Mining;
using MemPalace.Search;
using MemPalace.Agents;
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
        
        // Register IChatClient (local-first default)
        // Cloud opt-in can be added via manual DI override before building the app
        services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(sp =>
        {
            // Local-first default: ElBruno.LocalLLMs with Phi-3.5-mini (default model)
            try
            {
                // Use default Phi-3.5-mini from ElBruno.LocalLLMs (auto-downloads on first run)
                Console.WriteLine("[INFO] Initializing local LLM (Phi-3.5-mini)...");
                var client = ElBruno.LocalLLMs.LocalChatClient.CreateAsync().GetAwaiter().GetResult();
                Console.WriteLine("[INFO] Local LLM ready.");
                return client;
            }
            catch (Exception ex)
            {
                // Log warning and fall back to NoOp
                Console.Error.WriteLine($"[WARN] Failed to initialize local LLM: {ex.Message}");
                Console.Error.WriteLine($"[WARN] Falling back to NoOpMemorySummarizer (no LLM summarization)");
                return null!; // Will trigger NoOpMemorySummarizer fallback below
            }
        });
        
        // Register IChatClient (local-first default)
        // Cloud opt-in can be added via manual DI override before building the app
        services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(sp =>
        {
            // Local-first default: ElBruno.LocalLLMs with Phi-3.5-mini (default model)
            try
            {
                // Use default Phi-3.5-mini from ElBruno.LocalLLMs (auto-downloads on first run)
                Console.WriteLine("[INFO] Initializing local LLM (Phi-3.5-mini)...");
                var client = ElBruno.LocalLLMs.LocalChatClient.CreateAsync().GetAwaiter().GetResult();
                Console.WriteLine("[INFO] Local LLM ready.");
                return client;
            }
            catch (Exception ex)
            {
                // Log warning and fall back to NoOp
                Console.Error.WriteLine($"[WARN] Failed to initialize local LLM: {ex.Message}");
                Console.Error.WriteLine($"[WARN] Falling back to NoOpMemorySummarizer (no LLM summarization)");
                return null!; // Will trigger NoOpMemorySummarizer fallback below
            }
        });
        
        // Register wake-up summarization
        // If IChatClient is registered, use LLMMemorySummarizer; otherwise use NoOpMemorySummarizer
        services.AddSingleton<MemPalace.Ai.Summarization.IMemorySummarizer>(sp =>
        {
            var chatClient = sp.GetService<Microsoft.Extensions.AI.IChatClient>();
            if (chatClient != null)
            {
                return new MemPalace.Ai.Summarization.LLMMemorySummarizer(chatClient);
            }
            return new MemPalace.Ai.Summarization.NoOpMemorySummarizer();
        });

        
        // Register Agents
        services.AddMemPalaceAgents(o =>
            o.AgentsPath = Path.Combine(Directory.GetCurrentDirectory(), ".mempalace", "agents"));
        
        // Register Skill Manager
        services.AddSingleton<SkillManager>();
        
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
                .WithExample("mempalacenet mcp --transport stdio")
                .WithExample("mempalacenet mcp --transport sse --port 5050")
                .WithExample("mempalacenet mcp --transport both --port 5050");

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

            // Skills branch
            config.AddBranch("skill", skill =>
            {
                skill.SetDescription("Skill marketplace operations");
                
                skill.AddCommand<SkillListCommand>("list")
                    .WithDescription("List installed skills")
                    .WithExample("mempalacenet skill list");

                skill.AddCommand<SkillSearchCommand>("search")
                    .WithDescription("Search for skills (local only in Phase 1)")
                    .WithExample("mempalacenet skill search embedding")
                    .WithExample("mempalacenet skill search rag");

                skill.AddCommand<SkillInfoCommand>("info")
                    .WithDescription("Show skill details")
                    .WithExample("mempalacenet skill info my-skill");

                skill.AddCommand<SkillInstallCommand>("install")
                    .WithDescription("Install a skill from local path")
                    .WithExample("mempalacenet skill install ./my-skill")
                    .WithExample("mempalacenet skill install ~/Downloads/rag-skill");

                skill.AddCommand<SkillEnableCommand>("enable")
                    .WithDescription("Enable a skill")
                    .WithExample("mempalacenet skill enable my-skill");

                skill.AddCommand<SkillDisableCommand>("disable")
                    .WithDescription("Disable a skill")
                    .WithExample("mempalacenet skill disable my-skill");

                skill.AddCommand<SkillUninstallCommand>("uninstall")
                    .WithDescription("Uninstall a skill")
                    .WithExample("mempalacenet skill uninstall my-skill")
                    .WithExample("mempalacenet skill uninstall my-skill --force");

                skill.AddCommand<SkillMarketplaceSearchCommand>("marketplace-search")
                    .WithDescription("Search remote skill marketplace (requires MCP)")
                    .WithExample("mempalacenet skill marketplace-search rag")
                    .WithExample("mempalacenet skill marketplace-search vector-db");

                skill.AddCommand<SkillSourceListCommand>("source-list")
                    .WithDescription("List configured marketplace sources")
                    .WithExample("mempalacenet skill source-list");
            });
        });

        return app.Run(args);
    }
}
