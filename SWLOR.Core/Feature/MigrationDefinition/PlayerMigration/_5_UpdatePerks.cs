using SWLOR.Core.NWNX;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.Item;
using SWLOR.Core.Service;
using SWLOR.Core.Service.MigrationService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.MigrationDefinition.PlayerMigration
{
    public class _5_UpdatePerks: IPlayerMigration
    {
        public int Version => 5;
        public void Migrate(uint player)
        {
            var rightHandWeapon = GetItemInSlot(InventorySlot.RightHand, player);

            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
            Stat.ApplyAttacksPerRound(player, rightHandWeapon);

            var innerStrength = Perk.GetPerkLevel(player, PerkType.InnerStrength);
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
