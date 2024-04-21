using SWLOR.Core.NWNX;
using SWLOR.Core.NWScript.Enum;

namespace SWLOR.Core.Feature.MigrationDefinition.PlayerMigration
{
    public class _11_RemoveMobility : PlayerMigrationBase
    {
        public override int Version => 11;
        public override void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.Mobility);
        }
    }
}
