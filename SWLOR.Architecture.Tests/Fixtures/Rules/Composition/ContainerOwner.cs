using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Architecture.Tests.Fixtures.Rules.Composition;

/// <summary>Stands for the real Composition namespace, which is allowed to use the container.</summary>
public sealed class ContainerOwner
{
    public string Get(IServiceProvider provider) => provider.GetRequiredService<string>();
}
