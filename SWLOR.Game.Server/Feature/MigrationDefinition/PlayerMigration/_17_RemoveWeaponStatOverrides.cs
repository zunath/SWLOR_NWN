namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _17_RemoveWeaponStatOverrides : PlayerMigrationBase
{
    public override int Version => 17;

    public override void Migrate(uint player) => WeaponStatOverrideMigration.MigrateObject(player);
}
