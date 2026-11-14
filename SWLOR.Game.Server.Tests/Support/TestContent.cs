using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The content factory of tests without a container (MaintainabilityRefactorContentInjection.md, "Tests and tools"): a fixture
/// declares <c>private IContentFactory Content { get; } = TestContent.WithoutServices;</c>. It builds made content as the server's
/// factory does but passes null for every service parameter, so the object behaves as it did before it took services and a service
/// call fails with a null reference. No container is built; the server's registrations only name the service parameters.
/// </summary>
public static class TestContent
{
    private static readonly Lazy<IContentFactory> Factory = new(() =>
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        return ContentFactory.WithoutServices(services);
    });

    /// <summary>The factory that passes null for every service.</summary>
    public static IContentFactory WithoutServices => Factory.Value;
}
