using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MemPalace.Cli.Infrastructure;

internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return provider.GetService(type);
    }
}
