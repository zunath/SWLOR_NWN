using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _5_UpdatePerks: IPlayerMigration
    {
        public int Version => 5;
        public void Migrate(uint player)
        {
            var rightHandWeapon = GetItemInSlot(InventorySlot.RightHand, player);

            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
            var statService = ServiceContainer.GetService<IStatService>();
            statService.ApplyAttacksPerRound(player, rightHandWeapon);

            var perkService = ServiceContainer.GetService<IPerkService>();
            var innerStrength = perkService.GetPerkLevel(player, PerkType.InnerStrength);
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
