using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _12_RemoveDecayedPerks: PlayerMigrationBase
    {
        public _12_RemoveDecayedPerks(
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
        
        public override int Version => 12;
        public override void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = Database.Get<Player>(playerId);

            foreach (var (perkType, level) in dbPlayer.Perks)
            {
                var effectiveLevel = PerkService.GetPlayerEffectivePerkLevel(player, perkType);

                if (level != effectiveLevel)
                {
                    RefundPerk(player, perkType);
                }
            }
        }
    }
}
