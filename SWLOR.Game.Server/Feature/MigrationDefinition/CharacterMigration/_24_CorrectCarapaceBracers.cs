using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Removes the Carapace Bracer's and Carapace Wraps' retired Poison protection and gives droid parts and controllers
/// the Unarmed and Twin Pistol skills (<see cref="DroidWeaponSkillMigration"/>) in the combined inventory traversal.
/// The record gains a rebuild token for the Unarmed and Twin Pistol skills.
/// </summary>
internal sealed class _24_CorrectCarapaceBracers : ICharacterMigration
{
    public int Version => 24;
    public void Migrate(SavedCharacter character) => CharacterLootRevision.Migrate(character, updateArtwork: true);
    public void MigratePlayerData(Player player) => RebuildTokenGrant.Grant(player);
}
