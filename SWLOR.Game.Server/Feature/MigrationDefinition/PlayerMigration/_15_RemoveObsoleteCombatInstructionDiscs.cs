namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _15_RemoveObsoleteCombatInstructionDiscs : PlayerMigrationBase
    {
        public override int Version => 15;

        public override void Migrate(uint player)
        {
            ObsoleteItemMigration.RemoveObsoleteItemsFromObject(player);
            LegacySaberMigration.MigratePlayer(player);
            PlayerInitialization.ResetFeatsToBaseline(player);
        }
    }
}
