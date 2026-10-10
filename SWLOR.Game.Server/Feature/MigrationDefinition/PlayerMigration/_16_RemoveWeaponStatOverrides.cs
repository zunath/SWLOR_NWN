namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _16_RemoveWeaponStatOverrides : PlayerMigrationBase
{
    public override int Version => 16;

    public override void Migrate(uint player) => WeaponStatOverrideMigration.MigrateObject(player);
}
