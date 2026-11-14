using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The server's registrations built into a container of the test's own, for unit tests that run game code reaching a
/// converted service through its static facade (<c>Guild.GetGuild</c>, <c>PlayerMarket.GetBaseItemMarketCategory</c>).
/// In the server the facades resolve from the container the boot builds; a test installs this one for the current
/// flow with <see cref="Install"/>, and what the instances cache lives for as long as the container does.
/// </summary>
/// <remarks>
/// The test API of content injection (MaintainabilityRefactorContentInjection.md, "Tests and tools"): a fixture declares what its
/// code calls as properties of the same names, read from its own container, so test text matches content text and no test reaches
/// a service through a static lookup: <c>private IStatService Stat =&gt; _container.Get&lt;IStatService&gt;();</c> and
/// <c>private IContentFactory Content =&gt; _container.Content;</c>.
/// </remarks>
public sealed class FacadeContainer : IDisposable
{
    private static readonly AsyncLocal<FacadeContainer?> Installed = new();

    private readonly ServiceProvider _provider;

    public FacadeContainer()
        : this(null)
    {
    }

    /// <summary>
    /// Builds the server's registrations, then lets <paramref name="configure"/> replace some of them, for a test of
    /// the code that calls a facade (a handler that audits an item, say) and not of the service behind it:
    /// <c>services => services.Replace(ServiceDescriptor.Singleton&lt;IItemTrashService&gt;(recorder))</c>.
    /// </summary>
    public FacadeContainer(Action<IServiceCollection>? configure)
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        services.Replace(ServiceDescriptor.Singleton<IMessagingService, UnconcealedMessagingService>());
        configure?.Invoke(services);
        _provider = ContainerHost.Create(services);
    }

    /// <summary>
    /// Makes this container the one the facades resolve from, for the current async flow, until the returned scope is
    /// disposed. Install it in the same method that runs the code under test, or in a <c>[SetUp]</c>.
    /// </summary>
    public IDisposable Install()
    {
        var previous = Installed.Value;
        Installed.Value = this;
        return new InstalledScope(CompositionRoot.InstallForTests(_provider), previous);
    }

    /// <summary>The container installed for the current flow, for test helpers that reach an instance's private state.</summary>
    public static FacadeContainer Current =>
        Installed.Value ?? throw new InvalidOperationException("No FacadeContainer is installed for this test.");

    private sealed class InstalledScope : IDisposable
    {
        private readonly IDisposable _scope;
        private readonly FacadeContainer? _previous;

        public InstalledScope(IDisposable scope, FacadeContainer? previous)
        {
            _scope = scope;
            _previous = previous;
        }

        public void Dispose()
        {
            _scope.Dispose();
            Installed.Value = _previous;
        }
    }

    /// <summary>The container's instance of a service or content singleton, to call it, set it up or assert on it.</summary>
    public T Get<T>()
        where T : class => _provider.GetRequiredService<T>();

    /// <summary>Builds made content with this container's services (<c>Content.Create&lt;VenomStatusEffect, int, int&gt;(1, 1)</c>).</summary>
    public IContentFactory Content => _provider.GetRequiredService<IContentFactory>();

    /// <summary>True when this container registers <paramref name="type"/> as a service or content singleton.</summary>
    public bool IsRegistered(Type type) => ((ContentFactory)Content).IsRegistered(type);

    public void Dispose() => _provider.Dispose();
}

/// <summary>
/// The messaging service of the container for tests: no creature is concealed, because reading a creature's effects
/// goes through the native library, which unit tests do not load.
/// </summary>
internal sealed class UnconcealedMessagingService : MessagingService
{
    public UnconcealedMessagingService(IStealthService stealth)
        : base(stealth)
    {
    }

    protected override bool IsConcealed(uint creature) => false;
}
