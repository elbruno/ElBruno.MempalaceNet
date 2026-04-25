using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Infrastructure;

internal sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    private IServiceProvider? _serviceProvider;

    public ITypeResolver Build()
    {
        _serviceProvider ??= services.BuildServiceProvider();
        return new TypeResolver(_serviceProvider);
    }

    public void Register(Type service, Type implementation)
    {
        services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        services.AddSingleton(service, _ => factory());
    }
}
