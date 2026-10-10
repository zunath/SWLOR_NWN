namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _18_ReplaceDungeonSabers : PlayerMigrationBase
{
    public override int Version => 18;
    public override void Migrate(uint player) => DungeonSaberMigration.MigrateObject(player);
}
