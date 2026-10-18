namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Revises loot stats, including rare-item corrections, and repairs headgear in one inventory traversal.</summary>
public sealed class _22_ReviseLootStats : PlayerMigrationBase
{
    public override int Version => 22;

    public override void Migrate(uint player) => LootStatRevisionMigration.MigrateObject(player);
}
