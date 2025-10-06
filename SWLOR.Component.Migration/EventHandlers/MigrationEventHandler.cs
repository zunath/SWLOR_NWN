using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Migration.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Migration.EventHandlers
{
    /// <summary>
    /// Event handlers for the Migration component.
    /// </summary>
    public class MigrationEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public MigrationEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _migrationService = new Lazy<IMigrationService>(() => _serviceProvider.GetRequiredService<IMigrationService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IMigrationService> _migrationService;
        
        private IMigrationService MigrationService => _migrationService.Value;

        /// <summary>
        /// When the server loads, run database-related migrations.
        /// </summary>
        [ScriptHandler<OnServerLoaded>]
        public void AfterDatabaseLoaded()
        {
            MigrationService.AfterDatabaseLoaded();
        }

        /// <summary>
        /// When the module cache is loaded, run cache-related migrations.
        /// </summary>
        [ScriptHandler<OnModuleCacheAfter>]
        public void AfterCacheLoaded()
        {
            MigrationService.AfterCacheLoaded();
        }

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        [ScriptHandler<OnPlayerInitialized>]
        public void RunPlayerMigrations()
        {
            MigrationService.RunPlayerMigrations();
        }
    }
}
