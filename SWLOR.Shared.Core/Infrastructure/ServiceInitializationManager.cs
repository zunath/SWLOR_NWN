using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Infrastructure
{
    /// <summary>
    /// Manages the initialization of all services that require post-construction setup
    /// </summary>
    public class ServiceInitializationManager : IServiceInitializationManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly List<InitializationResult> _initializationResults = new();

        public class InitializationResult
        {
            public string ServiceName { get; set; }
            public bool Success { get; set; }
            public TimeSpan Duration { get; set; }
            public string ErrorMessage { get; set; }
            public int Priority { get; set; }
        }

        public ServiceInitializationManager(IServiceProvider serviceProvider, ILogger logger)
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
            var initializers = _serviceProvider.GetServices<IServiceInitializer>()
                .OrderBy(i => i.InitializationPriority)
                .ToList();

            _logger.Write<InfrastructureLogGroup>($"Found {initializers.Count} services requiring initialization");

            foreach (var initializer in initializers)
            {
                InitializeService(initializer);
            }

            LogInitializationResults();
        }

        /// <summary>
        /// Initializes a single service
        /// </summary>
        private void InitializeService(IServiceInitializer initializer)
        {
            var startTime = DateTime.UtcNow;
            var result = new InitializationResult
            {
                ServiceName = initializer.ServiceName,
                Priority = initializer.InitializationPriority
            };

            try
            {
                _logger.Write<InfrastructureLogGroup>($"Initializing {initializer.ServiceName} (Priority: {initializer.InitializationPriority})");

                initializer.Initialize();

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;

                _logger.Write<InfrastructureLogGroup>($"Successfully initialized {initializer.ServiceName} in {result.Duration.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Duration = DateTime.UtcNow - startTime;
                result.ErrorMessage = ex.Message;

                _logger.Write<InfrastructureLogGroup>($"Failed to initialize {initializer.ServiceName}: {ex.ToMessageAndCompleteStacktrace()}");
            }

            _initializationResults.Add(result);
        }

        /// <summary>
        /// Logs the initialization results
        /// </summary>
        private void LogInitializationResults()
        {
            var successful = _initializationResults.Count(r => r.Success);
            var failed = _initializationResults.Count(r => !r.Success);
            var totalTime = _initializationResults.Sum(r => r.Duration.TotalMilliseconds);

            _logger.Write<InfrastructureLogGroup>($"Service initialization completed: {successful} successful, {failed} failed, {totalTime:F2}ms total");

            if (failed > 0)
            {
                _logger.Write<InfrastructureLogGroup>("Failed service initializations:");
                foreach (var result in _initializationResults.Where(r => !r.Success))
                {
                    _logger.Write<InfrastructureLogGroup>($"  - {result.ServiceName}: {result.ErrorMessage}");
                }
            }

            // Log slow initializations
            var slowInitializations = _initializationResults
                .Where(r => r.Success && r.Duration.TotalMilliseconds > 1000)
                .OrderByDescending(r => r.Duration);

            if (slowInitializations.Any())
            {
                _logger.Write<InfrastructureLogGroup>("Slow service initializations (>1s):");
                foreach (var result in slowInitializations)
                {
                    _logger.Write<InfrastructureLogGroup>($"  - {result.ServiceName}: {result.Duration.TotalMilliseconds:F2}ms");
                }
            }
        }

        /// <summary>
        /// Gets the initialization results for monitoring
        /// </summary>
        public IReadOnlyList<InitializationResult> GetInitializationResults()
        {
            return _initializationResults.AsReadOnly();
        }
    }
}
