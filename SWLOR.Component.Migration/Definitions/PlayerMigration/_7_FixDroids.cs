using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _7_FixDroids: PlayerMigrationBase
    {
        public _7_FixDroids(
            ILogger logger,
            IDatabaseService database,
            IStatCalculationService statCalculationService,
            ISkillService skillService,
            ICombatService combatService,
            IPerkService perkService,
            IItemService itemService,
            ICreaturePluginService creaturePlugin,
            IStatApplicationService statApplicationService)
            : base(
                logger,
                database,
                statCalculationService,
                skillService,
                combatService,
                perkService,
                itemService,
                creaturePlugin, 
                statApplicationService)
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


