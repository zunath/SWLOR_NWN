using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Creates an object through the container's activator.</summary>
public sealed class ActivatorUser
{
    public object Make(IServiceProvider provider) => ActivatorUtilities.CreateInstance(provider, typeof(string));
}
