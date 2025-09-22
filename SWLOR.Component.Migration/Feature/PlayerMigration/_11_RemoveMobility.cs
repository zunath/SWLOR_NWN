using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
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
