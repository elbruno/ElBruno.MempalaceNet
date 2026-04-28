using MemPalace.Agents.Registry;
using MemPalace.Cli.Commands;
using MemPalace.Cli.Commands.Agents;
using MemPalace.Cli.Commands.Kg;
using MemPalace.Cli.Infrastructure;
using MemPalace.KnowledgeGraph;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console.Cli;

namespace MemPalace.Tests.Cli;

public sealed class CommandAppParseTests
{
    private static CommandApp CreateApp()
    {
        var services = new ServiceCollection();
        var fakeKg = Substitute.For<IKnowledgeGraph>();
        fakeKg.QueryAsync(Arg.Any<TriplePattern>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TemporalTriple>>(Array.Empty<TemporalTriple>()));
        fakeKg.TimelineAsync(Arg.Any<EntityRef>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TimelineEvent>>(Array.Empty<TimelineEvent>()));
        fakeKg.AddAsync(Arg.Any<TemporalTriple>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1L));
        fakeKg.CountAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
        services.AddSingleton<IKnowledgeGraph>(fakeKg);

        var fakeRegistry = Substitute.For<IAgentRegistry>();
        fakeRegistry.List().Returns(Array.Empty<MemPalace.Agents.AgentDescriptor>());
        services.AddSingleton<IAgentRegistry>(fakeRegistry);

        // Register required services for commands
        var fakeCollection = Substitute.For<MemPalace.Core.Backends.ICollection>();
        fakeCollection.GetAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<MemPalace.Core.Backends.WhereClause>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                Arg.Any<MemPalace.Core.Backends.IncludeFields>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MemPalace.Core.Backends.GetResult>(new MemPalace.Core.Backends.GetResult(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<IReadOnlyDictionary<string, object?>>())));

        var fakeBackend = Substitute.For<MemPalace.Core.Backends.IBackend>();
        fakeBackend.GetCollectionAsync(
                Arg.Any<MemPalace.Core.Model.PalaceRef>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<MemPalace.Core.Backends.IEmbedder>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MemPalace.Core.Backends.ICollection>(fakeCollection));
        services.AddSingleton<MemPalace.Core.Backends.IBackend>(fakeBackend);
        
        var fakeSummarizer = Substitute.For<MemPalace.Ai.Summarization.IMemorySummarizer>();
        services.AddSingleton<MemPalace.Ai.Summarization.IMemorySummarizer>(fakeSummarizer);

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InitCommand>("init");
            config.AddCommand<MineCommand>("mine");
            config.AddCommand<SearchCommand>("search");
            config.AddCommand<WakeUpCommand>("wake-up");
            
            config.AddBranch("agents", agents =>
            {
                agents.AddCommand<AgentsListCommand>("list");
            });
            
            config.AddBranch("kg", kg =>
            {
                kg.AddCommand<KgAddCommand>("add");
                kg.AddCommand<KgQueryCommand>("query");
                kg.AddCommand<KgTimelineCommand>("timeline");
            });
        });

        return app;
    }

    [Fact]
    public void InitCommand_ParsesPathArgument()
    {
        var app = CreateApp();
        var result = app.Run(["init", "./my-palace"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void InitCommand_ParsesWithNameOption()
    {
        var app = CreateApp();
        var result = app.Run(["init", "./my-palace", "--name", "TestPalace"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void SearchCommand_ParsesQueryWithOptions()
    {
        var app = CreateApp();
        var result = app.Run(["search", "hello", "--top-k", "5", "--rerank"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void SearchCommand_ParsesWingOption()
    {
        var app = CreateApp();
        var result = app.Run(["search", "test query", "--wing", "code"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void MineCommand_ParsesWithMode()
    {
        var app = CreateApp();
        
        // Create a temporary directory with a test file
        var testDir = Path.Combine(Path.GetTempPath(), "mempalace-cli-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        
        // Create a test file to mine
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "test content");
        
        try
        {
            var result = app.Run(["mine", testDir, "--mode", "convos", "--wing", "conversations"]);
            Assert.Equal(0, result);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }
    }

    [Fact]
    public void WakeUpCommand_RunsWithoutArguments()
    {
        var app = CreateApp();
        var result = app.Run(["wake-up"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AgentsListCommand_ParsesCorrectly()
    {
        var app = CreateApp();
        var result = app.Run(["agents", "list"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void KgAddCommand_ParsesTripleArguments()
    {
        var app = CreateApp();
        var result = app.Run(["kg", "add", "agent:tyrell", "worked-on", "project:MemPalace.Core"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void KgQueryCommand_ParsesPattern()
    {
        var app = CreateApp();
        var result = app.Run(["kg", "query", "? worked-on project:MemPalace.Core"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void KgTimelineCommand_ParsesEntity()
    {
        var app = CreateApp();
        var result = app.Run(["kg", "timeline", "agent:tyrell"]);
        Assert.Equal(0, result);
    }
}
