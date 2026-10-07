namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _16_UpdateItemIcons : PlayerMigrationBase
{
    public override int Version => 16;

    public override void Migrate(uint player) => ItemIconMigration.MigrateObject(player);
}
