using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _7_FixDroids: PlayerMigrationBase
    {
        public _7_FixDroids(ILogger logger, IDatabaseService database, IStatService statService, ISkillService skillService, ICombatService combatService, IPerkService perkService) 
            : base(logger, database, statService, skillService, combatService, perkService)
        {
        }
        
        public override int Version => 7;
        public override void Migrate(uint player)
        {
            RecalculateStats(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = Database.Get<Player>(playerId);
            var racialType = GetRacialType(player);

            if (racialType != RacialType.Droid)
                return;

            dbPlayer.OriginalAppearanceType = AppearanceType.Droid;

            _db.Set(dbPlayer);

            SetCreatureAppearanceType(player, AppearanceType.Droid);
        }
    }
}
