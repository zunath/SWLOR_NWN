using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _3_AdjustPlayerSpeeds: IPlayerMigration
    {
        public int Version => 3;
        public void Migrate(uint player)
        {
            Stat.ApplyPlayerMovementRate(player);
        }
    }
}
