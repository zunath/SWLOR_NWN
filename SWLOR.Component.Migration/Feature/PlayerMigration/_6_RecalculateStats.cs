using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _6_RecalculateStats: PlayerMigrationBase
    {
        public _6_RecalculateStats(
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
        
        public override int Version => 6;
        public override void Migrate(uint player)
        {
            RecalculateStats(player);
        }
    }
}
