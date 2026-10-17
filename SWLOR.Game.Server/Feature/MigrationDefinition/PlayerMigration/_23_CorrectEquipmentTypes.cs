namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Updates owned artwork, loot stats, wearable types and visor models in one inventory traversal.</summary>
public sealed class _23_CorrectEquipmentTypes : PlayerMigrationBase
{
    public override int Version => 23;
    public override void Migrate(uint player) => LootStatRevisionMigration.MigratePlayerItems(player);
}
