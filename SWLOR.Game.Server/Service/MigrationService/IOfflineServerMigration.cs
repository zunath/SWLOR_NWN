namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// A server migration that only reads and writes database records, so SWLOR.CLI can apply it to a
/// stopped server's database (see <see cref="OfflineServerMigrations"/>) instead of the server
/// running it at boot. <see cref="MigrateOffline"/> must produce the same records as
/// <see cref="IServerMigration.Migrate"/> without calling the game engine.
/// </summary>
public interface IOfflineServerMigration : IServerMigration
{
    void MigrateOffline(OfflineMigrationOptions options);
}
