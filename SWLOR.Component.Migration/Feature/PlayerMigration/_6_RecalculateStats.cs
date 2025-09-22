using SWLOR.Component.Migration.Model;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
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
