using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _5_UpdatePerks: IPlayerMigration
    {
        private readonly IStatService _statService;
        private readonly IPerkService _perkService;

        public _5_UpdatePerks(IStatService statService, IPerkService perkService)
        {
            _statService = statService;
            _perkService = perkService;
        }

        public int Version => 5;
        public void Migrate(uint player)
        {
            var rightHandWeapon = GetItemInSlot(InventorySlot.RightHand, player);

            CreaturePlugin.RemoveFeat(player, FeatType.RapidShot);
            _statService.ApplyAttacksPerRound(player, rightHandWeapon);

            var innerStrength = _perkService.GetPerkLevel(player, PerkType.InnerStrength);
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
