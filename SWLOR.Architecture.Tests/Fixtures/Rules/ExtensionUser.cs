using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Resolves through the dependency-injection extension methods.</summary>
public sealed class ExtensionUser
{
    public string Get(IServiceProvider provider) => provider.GetRequiredService<string>();
}
