using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Retires Force weapon enhancements on the character's equipped and carried items and droids, and renames
/// the retired enhancement recipes the record knows.
/// </summary>
internal sealed class _16_RetireForceWeaponEnhancements : ICharacterMigration
{
    public int Version => 16;
    public void Migrate(SavedCharacter character) => StoredObjectData.RemoveForceWeaponEnhancements(character.Root);
    public void MigratePlayerData(Player player) => ForceWeaponEnhancementMigration.MigrateRecipeKnowledge(player);
}
