using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The server's dependency-injection container. <see cref="ServerBootstrapper"/> builds <see cref="ServerHost"/> once
    /// at boot, in its <c>BuildServiceContainer</c> step, before the script registry loads any handler; nothing else builds it.
    /// Every game service is a singleton for the life of the process.
    /// </summary>
    /// <remarks>
    /// Game code never resolves services from here. The static facades of converted services do, through the
    /// internal <see cref="FacadeAccessor"/>, and an architecture test (SWLOR.Architecture.Tests) fails when anything
    /// else does. Tests that need their own container install it with <see cref="InstallForTests"/>.
    /// </remarks>
    public static class CompositionRoot
    {
        private static readonly AsyncLocal<OverrideScope> Overridden = new AsyncLocal<OverrideScope>();
        private static int _activeOverrides;

        /// <summary>The server's container, built at boot.</summary>
        public static ContainerHost ServerHost { get; } = new ContainerHost();

        /// <summary>True once the server's container has been built.</summary>
        public static bool IsBuilt => ServerHost.IsBuilt;

        /// <summary>
        /// The container facades resolve from: the one a test installed for the current async flow, or the server's.
        /// Throws <see cref="ContainerNotBuiltException"/> when neither exists.
        /// </summary>
        internal static IServiceProvider Current
        {
            get
            {
                if (Volatile.Read(ref _activeOverrides) != 0)
                {
                    var overridden = FindOverride();
                    if (overridden != null)
                        return overridden;
                }

                return ServerHost.Provider;
            }
        }

        /// <summary>
        /// Builds <paramref name="host"/> from the server's registrations. <see cref="ServerBootstrapper"/> calls it with
        /// <see cref="ServerHost"/> in its <c>BuildServiceContainer</c> step; tests call it with a host of their own.
        /// </summary>
        /// <returns>The number of registrations.</returns>
        public static int Build(ContainerHost host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            return host.Build(ServiceRegistrations.Register);
        }

        /// <summary>
        /// Registers the server's services into <paramref name="services"/>: the one list the boot builds the container
        /// from. Tests start from this and replace the registrations they want to fake.
        /// </summary>
        public static void RegisterServerServices(IServiceCollection services)
        {
            ServiceRegistrations.Register(services);
        }

        /// <summary>
        /// Test-only. Makes <paramref name="provider"/> the container facades resolve from, for the current async flow,
        /// until the returned scope is disposed. The override lives in an <see cref="AsyncLocal{T}"/>, so tests running
        /// in parallel do not see each other's containers. Production code never calls this.
        /// </summary>
        public static IDisposable InstallForTests(IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var scope = new OverrideScope(provider, Overridden.Value);
            Overridden.Value = scope;
            Interlocked.Increment(ref _activeOverrides);
            return scope;
        }

        private static IServiceProvider FindOverride()
        {
            for (var scope = Overridden.Value; scope != null; scope = scope.Previous)
            {
                if (scope.IsActive)
                    return scope.Provider;
            }

            return null;
        }

        private sealed class OverrideScope : IDisposable
        {
            private int _disposed;

            public OverrideScope(IServiceProvider provider, OverrideScope previous)
            {
                Provider = provider;
                Previous = previous;
            }

            public IServiceProvider Provider { get; }

            public OverrideScope Previous { get; }

            public bool IsActive => Volatile.Read(ref _disposed) == 0;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    Interlocked.Decrement(ref _activeOverrides);
                    if (ReferenceEquals(Overridden.Value, this))
                        Overridden.Value = Previous;
                }
            }
        }
    }
}
