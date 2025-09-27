using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _5_UpdatePerks: PlayerMigrationBase
    {
        public _5_UpdatePerks(
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

        public override int Version => 5;
        public override void Migrate(uint player)
        {
            var rightHandWeapon = GetItemInSlot(InventorySlot.RightHand, player);

            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
            StatService.ApplyAttacksPerRound(player, rightHandWeapon);

            var innerStrength = PerkService.GetPerkLevel(player, PerkType.InnerStrength);
            if (innerStrength > 0)
            {
                // Remove old one which only targeted gloves.
                CreaturePlugin.SetCriticalRangeModifier(player, 0, 0, true, BaseItem.Gloves);

                // Apply new one which targets all weapons
                CreaturePlugin.SetCriticalRangeModifier(player, -innerStrength, 0, true);
            }
        }
    }
}
