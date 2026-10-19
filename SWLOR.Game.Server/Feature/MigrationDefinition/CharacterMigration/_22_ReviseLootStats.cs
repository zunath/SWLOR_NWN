using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Revises loot stats, including rare-item corrections, and repairs headgear in one inventory traversal.</summary>
internal sealed class _22_ReviseLootStats : ICharacterMigration
{
    public int Version => 22;
    public void Migrate(SavedCharacter character) => CharacterLootRevision.Migrate(character, updateArtwork: false);
}
