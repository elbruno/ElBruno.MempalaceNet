using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using CustomSkill;

// Build configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables("CUSTOM_SKILL_")
    .Build();

// Setup DI container
var services = new ServiceCollection();

// Register configuration explicitly as IConfiguration interface
services.AddSingleton<IConfiguration>(config);

// Register core services
services.AddScoped<ICustomSkillService, CustomSkillService>();

// Build the provider once
var provider = services.BuildServiceProvider();

// Create the CLI app with custom registrar
var registrar = new SkillTypeRegistrar(provider);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("custom-skill");
    config.SetApplicationVersion(GetVersion());
    config.ValidateExamples();

    config
        .AddCommand<CustomSkillCommand>("custom-skill")
        .WithDescription("Run custom skill with semantic search")
        .WithExample(new[] { "custom-skill", "your query here" })
        .WithExample(new[] { "custom-skill", "kubernetes", "--wing", "ops", "--limit", "5" });
});

try
{
    return await app.RunAsync(args);
}
finally
{
    provider.Dispose();
}

/// <summary>
/// Gets the assembly version for display.
/// </summary>
static string GetVersion()
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "1.0.0";
    return version;
}

/// <summary>
/// Custom type registrar for Spectre CLI that uses dependency injection.
/// </summary>
public sealed class SkillTypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _provider;

    public SkillTypeRegistrar(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ITypeResolver Build() => new SkillTypeResolver(_provider);

    public void Register(Type service, Type implementation)
    {
        // No-op for runtime registration
    }

    public void Register(Type service, Func<IServiceProvider, object> factory)
    {
        // No-op for runtime registration
    }

    public void RegisterInstance(Type service, object instance)
    {
        // No-op for runtime registration
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // No-op for runtime registration
    }
}

/// <summary>
/// Custom type resolver that uses the service provider to resolve commands and their dependencies.
/// </summary>
public sealed class SkillTypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public SkillTypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
            return null;

        try
        {
            // Try to get from service provider first
            var instance = _provider.GetService(type);
            if (instance != null)
                return instance;

            // For commands and other types, try to create with DI support
            return ActivatorUtilities.CreateInstance(_provider, type);
        }
        catch (Exception ex)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error resolving {type.Name}: {ex.Message}[/]");
            return null;
        }
    }
}


