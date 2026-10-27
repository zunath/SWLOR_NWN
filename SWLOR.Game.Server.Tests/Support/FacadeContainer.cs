using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The server's registrations built into a container of the test's own, for unit tests that run game code reaching a
/// converted service through its static facade (<c>Guild.GetGuild</c>, <c>PlayerMarket.GetBaseItemMarketCategory</c>).
/// In the server the facades resolve from the container the boot builds; a test installs this one for the current
/// flow with <see cref="Install"/>, and what the instances cache lives for as long as the container does.
/// </summary>
public sealed class FacadeContainer : IDisposable
{
    private readonly ServiceProvider _provider;

    public FacadeContainer()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        _provider = ContainerHost.Create(services);
    }

    /// <summary>
    /// Makes this container the one the facades resolve from, for the current async flow, until the returned scope is
    /// disposed. Install it in the same method that runs the code under test, or in a <c>[SetUp]</c>.
    /// </summary>
    public IDisposable Install() => CompositionRoot.InstallForTests(_provider);

    /// <summary>The container's instance of a service, to set it up or to assert on it.</summary>
    public TService Get<TService>()
        where TService : notnull => _provider.GetRequiredService<TService>();

    public void Dispose() => _provider.Dispose();
}
