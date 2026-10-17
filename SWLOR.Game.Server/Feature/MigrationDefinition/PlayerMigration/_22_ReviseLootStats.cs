namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Updates artwork, loot stats, wearable types and visor models in one inventory traversal.</summary>
public sealed class _22_ReviseLootStats : PlayerMigrationBase
{
    public override int Version => 22;

    public override void Migrate(uint player) => LootStatRevisionMigration.MigratePlayerItems(player);
}
