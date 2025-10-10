using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
{
    public class _11_RemoveMobility : PlayerMigrationBase
    {
        public _11_RemoveMobility(
            ILogger logger,
            IDatabaseService database,
            IStatCalculationService statCalculationService,
            ISkillService skillService,
            IPerkService perkService,
            IItemService itemService,
            ICreaturePluginService creaturePlugin,
            IStatApplicationService statApplicationService)
            : base(
                logger, 
                database, 
                statCalculationService, 
                skillService,
                perkService,
                itemService, 
                creaturePlugin,
                statApplicationService)
        {
        }
        
        public override int Version => 11;
        public override void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.Mobility);
        }
    }
}


