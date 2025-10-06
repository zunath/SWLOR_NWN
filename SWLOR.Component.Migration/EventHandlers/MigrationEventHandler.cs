using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Migration.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Migration.EventHandlers
{
    /// <summary>
    /// Event handlers for the Migration component.
    /// </summary>
    public class MigrationEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MigrationEventHandler(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _migrationService = new Lazy<IMigrationService>(() => _serviceProvider.GetRequiredService<IMigrationService>());

            // Subscribe to events
            eventAggregator.Subscribe<OnServerLoaded>(e => AfterDatabaseLoaded());
            eventAggregator.Subscribe<OnModuleCacheAfter>(e => AfterCacheLoaded());
            eventAggregator.Subscribe<OnPlayerCacheData>(e => RunPlayerMigrations());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IMigrationService> _migrationService;
        
        private IMigrationService MigrationService => _migrationService.Value;

        /// <summary>
        /// When the server loads, run database-related migrations.
        /// </summary>
        public void AfterDatabaseLoaded()
        {
            MigrationService.AfterDatabaseLoaded();
        }

        /// <summary>
        /// When the module cache is loaded, run cache-related migrations.
        /// </summary>
        public void AfterCacheLoaded()
        {
            MigrationService.AfterCacheLoaded();
        }

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        public void RunPlayerMigrations()
        {
            MigrationService.RunPlayerMigrations();
        }
    }
}
