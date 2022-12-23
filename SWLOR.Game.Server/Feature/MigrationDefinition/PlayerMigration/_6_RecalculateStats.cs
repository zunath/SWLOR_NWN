namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _6_RecalculateStats: PlayerMigrationBase
    {
        public override int Version => 6;
        public override void Migrate(uint player)
        {
            RecalculateStats(player);
        }
    }
}
