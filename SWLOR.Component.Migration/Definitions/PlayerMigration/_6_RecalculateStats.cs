using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _6_RecalculateStats: PlayerMigrationBase
    {
        public _6_RecalculateStats(
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
        
        public override int Version => 6;
        public override void Migrate(uint player)
        {
            RecalculateStats(player);
        }
    }
}


