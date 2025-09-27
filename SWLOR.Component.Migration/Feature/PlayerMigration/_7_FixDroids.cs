using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _7_FixDroids: PlayerMigrationBase
    {
        public _7_FixDroids(
            ILogger logger, 
            IDatabaseService database, 
            IStatService statService, 
            ISkillService skillService, 
            ICombatService combatService, 
            IPerkService perkService,
            IItemService itemService) 
            : base(
                logger, 
                database, 
                statService, 
                skillService, 
                combatService, 
                perkService, 
                itemService)
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

            Database.Set(dbPlayer);

            SetCreatureAppearanceType(player, AppearanceType.Droid);
        }
    }
}
