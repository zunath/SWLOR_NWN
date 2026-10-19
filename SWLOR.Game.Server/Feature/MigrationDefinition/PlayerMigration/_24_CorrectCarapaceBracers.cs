namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _24_CorrectCarapaceBracers : PlayerMigrationBase
{
    public override int Version => 24;
    public override void Migrate(uint player) => LootStatRevisionMigration.MigratePlayerItems(player);
}
