using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Removes the Carapace Bracer's retired Poison protection in the combined inventory traversal.</summary>
internal sealed class _24_CorrectCarapaceBracers : ICharacterMigration
{
    public int Version => 24;
    public void Migrate(SavedCharacter character) => CharacterLootRevision.Migrate(character, updateArtwork: true);
}
