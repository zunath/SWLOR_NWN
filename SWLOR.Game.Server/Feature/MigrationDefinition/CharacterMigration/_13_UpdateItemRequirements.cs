using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Moves legacy item requirements to skill ranks and converts droid boost items and blueprints.</summary>
internal sealed class _13_UpdateItemRequirements : ICharacterMigration
{
    public int Version => 13;

    public void Migrate(SavedCharacter character)
    {
        SavedEquipmentRequirementMigration.MigrateCharacter(character);
        SavedDroidBoostItemMigration.MigrateCharacter(character);
    }
}
