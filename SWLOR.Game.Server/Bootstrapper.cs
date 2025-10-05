using System;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server
{
    public static class Bootstrapper
    {
        public static void Bootstrap()
        {
            var serviceProvider = InitializeDependencyInjection();

            // Bootstrap the server
            var serverManager = serviceProvider.GetRequiredService<IServerManager>();
            serverManager.Bootstrap();
        }

        /// <summary>
        /// Initializes the dependency injection container with all game services.
        /// </summary>
        private static IServiceProvider InitializeDependencyInjection()
        {
            var services = new ServiceCollection();
            
            // Register all game services
            services.AddServices();
            
            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Initialize the static service container
            ServiceContainer.Initialize(serviceProvider);
            
            return serviceProvider;
        }
    }
}
