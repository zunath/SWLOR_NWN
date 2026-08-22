namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _14_MigrateResistanceItemProperties : PlayerMigrationBase
    {
        public override int Version => 14;

        public override void Migrate(uint player)
        {
            SerializedItemResistanceMigration.MigrateObject(player);
            SerializedItemWeaponDamageTypeMigration.MigrateObject(player);
            CombatReadinessMigration.MigratePlayer(player);
            PistolBaseItemMigration.MigratePlayer(player);
        }
    }
}
