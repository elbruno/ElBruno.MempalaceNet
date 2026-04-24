using Spectre.Console.Cli;

namespace MemPalace.Cli.Infrastructure;

internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    public object? Resolve(Type? type)
    {
        return type == null ? null : provider.GetService(type);
    }
}
