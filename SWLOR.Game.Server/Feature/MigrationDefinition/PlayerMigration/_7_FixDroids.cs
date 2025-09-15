using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _7_FixDroids: PlayerMigrationBase
    {
        public override int Version => 7;
        public override void Migrate(uint player)
        {
            RecalculateStats(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var racialType = GetRacialType(player);

            if (racialType != RacialType.Droid)
                return;

            dbPlayer.OriginalAppearanceType = AppearanceType.Droid;

            DB.Set(dbPlayer);

            SetCreatureAppearanceType(player, AppearanceType.Droid);
        }
    }
}
