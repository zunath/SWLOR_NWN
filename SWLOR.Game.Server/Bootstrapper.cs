using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Core.Server;

namespace SWLOR.Game.Server
{
    public static class Bootstrapper
    {
        public static void Bootstrap()
        {
            // Initialize dependency injection container
            InitializeDependencyInjection();
            
            // Bootstrap the server
            ServerManager.Bootstrap();
        }

        /// <summary>
        /// Initializes the dependency injection container with all game services.
        /// </summary>
        private static void InitializeDependencyInjection()
        {
            var services = new ServiceCollection();
            
            // Register all game services
            services.AddGameServices();
            
            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Initialize the static service container
            ServiceContainer.Initialize(serviceProvider);
        }
    }
}
