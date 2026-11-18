using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The next release's player migration, empty until a change needs it. Put every character file and record change
/// for that release here, in one item traversal, and rename the class after what it does (<c>Migrations.md</c>).
/// </summary>
internal sealed class _27_NextReleaseCharacterMigration : ICharacterMigration
{
    public int Version => 27;

    public void Migrate(SavedCharacter character) { }

    public void MigratePlayerData(Player player) { }
}
