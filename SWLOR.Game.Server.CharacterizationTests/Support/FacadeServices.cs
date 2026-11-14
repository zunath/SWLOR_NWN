using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// The server's registrations built into a container, for characterization tests that build definitions which read a
/// converted service through its static facade (the venom recipes ask <c>Poisons</c> for their numbers). The server
/// builds this container at boot; a test installs it for the current flow with <see cref="Install"/>.
/// </summary>
public sealed class FacadeServices : IDisposable
{
    private readonly ServiceProvider _provider;

    public FacadeServices()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        _provider = ContainerHost.Create(services);
    }

    /// <summary>Makes this container the one the facades resolve from until the returned scope is disposed.</summary>
    public IDisposable Install() => CompositionRoot.InstallForTests(_provider);

    /// <summary>The container's instance of a service, for a test that reaches its state.</summary>
    public TService Get<TService>()
        where TService : notnull => _provider.GetRequiredService<TService>();

    public void Dispose() => _provider.Dispose();
}
