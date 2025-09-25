namespace SWLOR.Component.Migration.Contracts
{
    public interface IMigrationService
    {
        /// <summary>
        /// When the server loads, run database-related migrations.
        /// </summary>
        void AfterDatabaseLoaded();

        /// <summary>
        /// When the module cache is loaded, run cache-related migrations.
        /// </summary>
        void AfterCacheLoaded();

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        void RunPlayerMigrations();

        /// <summary>
        /// Runs server migrations after cache is loaded.
        /// </summary>
        void RunServerMigrationsPostCache();

        /// <summary>
        /// Retrieves the latest migration version for players.
        /// </summary>
        /// <returns>The latest migration version for players.</returns>
        int GetLatestPlayerVersion();
    }
}
