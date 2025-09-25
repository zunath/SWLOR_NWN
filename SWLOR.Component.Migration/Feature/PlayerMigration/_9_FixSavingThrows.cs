using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _9_FixSavingThrows: PlayerMigrationBase
    {
        public _9_FixSavingThrows(
            ILogger logger, 
            IDatabaseService database, 
            IStatService statService, 
            ISkillService skillService, 
            ICombatService combatService, 
            IPerkService perkService, 
            IItemService itemService) 
            : base(logger, database, statService, skillService, combatService, perkService, itemService)
        {
        }
        
        public override int Version => 9;
        public override void Migrate(uint player)
        {
            // Addresses a problem with player saving throws being incorrect due to changes in the 2DA files.
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Fortitude, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Will, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Reflex, 0);
        }
    }
}
