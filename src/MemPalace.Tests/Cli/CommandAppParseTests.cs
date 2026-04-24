using MemPalace.Cli.Commands;
using MemPalace.Cli.Commands.Kg;
using MemPalace.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MemPalace.Tests.Cli;

public sealed class CommandAppParseTests
{
    private static CommandApp CreateApp()
    {
        var services = new ServiceCollection();
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
        var result = app.Run(["mine", "./path", "--mode", "convos", "--wing", "conversations"]);
        Assert.Equal(0, result);
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
        var result = app.Run(["kg", "add", "subject", "predicate", "object"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void KgQueryCommand_ParsesPattern()
    {
        var app = CreateApp();
        var result = app.Run(["kg", "query", "? worked-on Project"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void KgTimelineCommand_ParsesEntity()
    {
        var app = CreateApp();
        var result = app.Run(["kg", "timeline", "Tyrell"]);
        Assert.Equal(0, result);
    }
}
