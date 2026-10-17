namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Corrects owned wearable types, including characters who already received the loot stat revision.</summary>
public sealed class _23_CorrectEquipmentTypes : PlayerMigrationBase
{
    public override int Version => 23;
    public override void Migrate(uint player) => LootStatRevisionMigration.MigrateObject(player);
}
