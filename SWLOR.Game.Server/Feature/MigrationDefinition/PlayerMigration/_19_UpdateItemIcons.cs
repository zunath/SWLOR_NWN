namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _19_UpdateItemIcons : PlayerMigrationBase
{
    public override int Version => 19;

    public override void Migrate(uint player) => ItemIconMigration.MigrateObject(player);
}
