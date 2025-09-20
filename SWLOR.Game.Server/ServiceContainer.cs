using System;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server
{
    /// <summary>
    /// Static service container for dependency injection in the game server.
    /// Provides access to registered services throughout the application.
    /// </summary>
    public static class ServiceContainer
    {
        private static IServiceProvider? _serviceProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the service provider instance.
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    lock (_lock)
                    {
                        if (_serviceProvider == null)
                        {
                            throw new InvalidOperationException("Service container has not been initialized. Call Initialize() first.");
                        }
                    }
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        /// Initializes the service container with the provided service provider.
        /// </summary>
        /// <param name="serviceProvider">The configured service provider</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            lock (_lock)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance</returns>
        public static T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a service of the specified type, or null if not registered.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance or null</returns>
        public static T GetServiceOptional<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Checks if the service container has been initialized.
        /// </summary>
        /// <returns>True if initialized, false otherwise</returns>
        public static bool IsInitialized => _serviceProvider != null;
    }
}
