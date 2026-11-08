using System.Collections.Generic;
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

        // The service types each singleton class is registered under (implementation type -> service types), set before
        // the provider is published and never changed after.
        private IReadOnlyDictionary<Type, Type[]> _registeredClasses = new Dictionary<Type, Type[]>();

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
                var provider = Create(services);
                _registeredClasses = services
                    .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton && !descriptor.ServiceType.IsGenericTypeDefinition)
                    .Select(descriptor => (Class: descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType(), descriptor.ServiceType))
                    .Where(entry => entry.Class != null)
                    .GroupBy(entry => entry.Class)
                    .ToDictionary(group => group.Key, group => group.Select(entry => entry.ServiceType).ToArray());
                Volatile.Write(ref _provider, provider);
                return services.Count;
            }
        }

        /// <summary>
        /// The instance the script registry binds the event handlers of <paramref name="type"/> to: the container's
        /// singleton of that class, the same one the services and <see cref="GameServices"/> resolve. Null when the
        /// container does not register the class. <see cref="ServerBootstrapper"/> passes this to the script registry,
        /// so the registry never touches the container itself.
        /// </summary>
        /// <exception cref="ContainerNotBuiltException">The container is not built.</exception>
        /// <exception cref="InvalidOperationException">
        /// The class is registered under more than one service type: each registration is a singleton of its own, so a
        /// handler would run on one of several instances.
        /// </exception>
        public object ResolveHandlerOwner(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var provider = Provider;
            if (!_registeredClasses.TryGetValue(type, out var serviceTypes))
                return null;

            if (serviceTypes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{type.FullName} is registered as {string.Join(", ", serviceTypes.Select(serviceType => serviceType.FullName))}: each " +
                    "registration is a singleton of its own, so its event handlers would run on one of several instances. Register the class once.");
            }

            return provider.GetRequiredService(serviceTypes[0]);
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
