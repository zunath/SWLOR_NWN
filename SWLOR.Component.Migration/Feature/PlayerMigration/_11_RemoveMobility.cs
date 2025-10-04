using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _11_RemoveMobility : PlayerMigrationBase
    {
        public _11_RemoveMobility(
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
        
        public override int Version => 11;
        public override void Migrate(uint player)
        {
            CreaturePlugin.RemoveFeat(player, FeatType.Mobility);
        }
    }
}
