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
    public class _9_FixSavingThrows: PlayerMigrationBase
    {
        public _9_FixSavingThrows(
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
        
        public override int Version => 9;
        public override void Migrate(uint player)
        {
            // Addresses a problem with player saving throws being incorrect due to changes in the 2DA files.
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrowCategoryType.Fortitude, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrowCategoryType.Will, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrowCategoryType.Reflex, 0);
        }
    }
}


