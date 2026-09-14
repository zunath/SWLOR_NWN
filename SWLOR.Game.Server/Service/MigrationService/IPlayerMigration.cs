using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.MigrationService
{
    public interface IPlayerMigration
    {
        int Version { get; }
        void Migrate(uint player);

        /// <summary>
        /// Updates the refreshed player record after live-object migration succeeds.
        /// The runner saves these changes together with Player.Version.
        /// </summary>
        void MigratePlayerData(Player player) { }
    }
}
