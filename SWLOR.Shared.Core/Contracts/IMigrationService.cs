namespace SWLOR.Shared.Core.Contracts
{
    public interface IMigrationService
    {
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
