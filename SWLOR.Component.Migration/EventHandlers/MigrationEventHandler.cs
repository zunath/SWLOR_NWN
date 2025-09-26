using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Migration.EventHandlers
{
    /// <summary>
    /// Event handlers for the Migration component.
    /// </summary>
    public class MigrationEventHandler
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IMigrationService MigrationService => _serviceProvider.GetRequiredService<IMigrationService>();

        public MigrationEventHandler(IServiceProvider serviceProvider)
        {
            // Services are now lazy-loaded via IServiceProvider
        }

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
        [ScriptHandler<OnCharacterInitAfter>]
        public void RunPlayerMigrations()
        {
            MigrationService.RunPlayerMigrations();
        }
    }
}
