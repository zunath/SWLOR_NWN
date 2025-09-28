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

            // Initialize all services that require post-construction setup
            InitializeServices(serviceProvider);

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

        /// <summary>
        /// Initializes all services that require post-construction setup
        /// </summary>
        private static void InitializeServices(IServiceProvider serviceProvider)
        {
            try
            {
                var initializationManager = serviceProvider.GetRequiredService<ServiceInitializationManager>();
                initializationManager.InitializeAllServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during service initialization: {ex.Message}");
                throw;
            }
        }
    }
}
