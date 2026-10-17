namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Repairs reserved visor models and artwork for characters already checkpointed through migration 23.</summary>
public sealed class _24_UpdateItemAppearances : PlayerMigrationBase
{
    public override int Version => 24;

    public override void Migrate(uint player) => LootStatRevisionMigration.MigratePlayerItems(player);
}
