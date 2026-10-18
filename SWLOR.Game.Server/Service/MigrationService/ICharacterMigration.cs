using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Upgrades every saved character file and its player record at boot, before players connect, including
/// characters whose players have not logged in since. Migrations run in version order on worker threads, so
/// they change saved data only: no NWScript, NWNX, engine or database calls.
/// </summary>
internal interface ICharacterMigration
{
    int Version { get; }

    /// <summary>
    /// Changes the character file. Runs when the file is behind this version. Changes to the record that follow
    /// from file changes (such as equipped item stats) go through <paramref name="character"/>, which skips them
    /// when a retried file's record already has them.
    /// </summary>
    void Migrate(SavedCharacter character);

    /// <summary>Changes only the record. Runs when the record is behind this version.</summary>
    void MigratePlayerData(Player player) { }
}
