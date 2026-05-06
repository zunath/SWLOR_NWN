using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _5_UpdatePerks: IPlayerMigration
    {
        public int Version => 5;
        public void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
        }
    }
}
