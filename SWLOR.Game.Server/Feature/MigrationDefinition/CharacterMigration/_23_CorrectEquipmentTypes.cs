using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Updates owned artwork, loot stats, wearable types and visor models in one inventory traversal.</summary>
internal sealed class _23_CorrectEquipmentTypes : ICharacterMigration
{
    public int Version => 23;
    public void Migrate(SavedCharacter character) => CharacterLootRevision.Migrate(character, updateArtwork: true);
}
