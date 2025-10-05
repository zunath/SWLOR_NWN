using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Infrastructure
{
    /// <summary>
    /// Manages the initialization of all services that require post-construction setup
    /// </summary>
    public class ServiceInitializer : IServiceInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public ServiceInitializer(IServiceProvider serviceProvider, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Initializes all services that implement IServiceInitializer
        /// </summary>
        public void InitializeAllServices()
        {
            _logger.Write<InfrastructureLogGroup>("Starting service initialization...");

            // Get all services that implement IServiceInitializer
            var initializers = _serviceProvider.GetServices<IInitializable>()
                .ToList();

            _logger.Write<InfrastructureLogGroup>($"Found {initializers.Count} services requiring initialization");

            foreach (var initializer in initializers)
            {
                InitializeService(initializer);
            }

        }

        /// <summary>
        /// Initializes a single service
        /// </summary>
        private void InitializeService(IInitializable initializer)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.Write<InfrastructureLogGroup>($"Initializing {initializer.GetType()}");

                initializer.Initialize();

                _logger.Write<InfrastructureLogGroup>($"Successfully initialized {initializer.GetType()}");
            }
            catch (Exception ex)
            {

                _logger.Write<InfrastructureLogGroup>($"Failed to initialize {initializer.GetType()}: {ex.ToMessageAndCompleteStacktrace()}");
            }
        }
    }
}
