using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Holds one dependency-injection container that is built once. The server has exactly one, held by
    /// <see cref="CompositionRoot"/>; tests make their own to prove the build rules without touching the server's.
    /// </summary>
    public sealed class ContainerHost
    {
        private readonly object _buildLock = new object();
        private ServiceProvider _provider;

        /// <summary>True once <see cref="Build"/> has completed.</summary>
        public bool IsBuilt => Volatile.Read(ref _provider) != null;

        /// <summary>
        /// The built container. Throws <see cref="ContainerNotBuiltException"/> when <see cref="Build"/> has not completed.
        /// </summary>
        public IServiceProvider Provider
        {
            get
            {
                var provider = Volatile.Read(ref _provider);
                if (provider == null)
                {
                    throw new ContainerNotBuiltException(
                        "The service container has not been built yet. A converted service was used before " +
                        "ServerBootstrapper ran its BuildServiceContainer step, which must come before the script handlers load.");
                }

                return provider;
            }
        }

        /// <summary>
        /// Registers the services with <paramref name="register"/> and builds the container. It is built once:
        /// a second call throws <see cref="ContainerNotBuiltException"/> and leaves the first container in place.
        /// </summary>
        /// <returns>The number of registrations.</returns>
        public int Build(Action<IServiceCollection> register)
        {
            if (register == null)
                throw new ArgumentNullException(nameof(register));

            lock (_buildLock)
            {
                if (_provider != null)
                {
                    throw new ContainerNotBuiltException(
                        "The service container was already built. It is built once, at boot, by ServerBootstrapper.");
                }

                var services = new ServiceCollection();
                register(services);
                Volatile.Write(ref _provider, Create(services));
                return services.Count;
            }
        }

        /// <summary>
        /// Builds a container with the options the server uses. Every registration is checked when the container is
        /// built (a missing dependency or a singleton that depends on a scoped service fails now, not on first use) and
        /// every singleton is created now, so a constructor that throws stops the boot instead of the first handler that
        /// needs the service.
        /// </summary>
        public static ServiceProvider Create(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

            try
            {
                foreach (var serviceType in services
                             .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton && !descriptor.ServiceType.IsGenericTypeDefinition)
                             .Select(descriptor => descriptor.ServiceType)
                             .Distinct())
                {
                    provider.GetRequiredService(serviceType);
                }
            }
            catch
            {
                provider.Dispose();
                throw;
            }

            return provider;
        }
    }
}
