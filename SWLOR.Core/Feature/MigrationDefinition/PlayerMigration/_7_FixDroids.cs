using SWLOR.Core.Entity;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;

namespace SWLOR.Core.Feature.MigrationDefinition.PlayerMigration
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
