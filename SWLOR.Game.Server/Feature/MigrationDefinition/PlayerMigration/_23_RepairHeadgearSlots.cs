namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _23_RepairHeadgearSlots : PlayerMigrationBase
{
    public override int Version => 23;
    public override void Migrate(uint player) => HeadgearSlotMigration.MigrateObject(player);
}
