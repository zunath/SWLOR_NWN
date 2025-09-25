using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Perk.Service
{
    /// <summary>
    /// Service for handling perk-specific effects and behaviors.
    /// </summary>
    public class PerkEffectService : IPerkEffectService
    {
        private readonly IPerkService _perkService;
        private readonly IStatService _statService;
        private readonly IItemService _itemService;
        private readonly IRandomService _random;
        private readonly IBeastMasteryService _beastMastery;

        public PerkEffectService(
            IPerkService perkService,
            IStatService statService,
            IItemService itemService,
            IRandomService random,
            IBeastMasteryService beastMastery)
        {
            _perkService = perkService;
            _statService = statService;
            _itemService = itemService;
            _random = random;
            _beastMastery = beastMastery;
        }

        /// <summary>
        /// When a weapon hits, apply Alacrity and Clarity effects for OneHanded perks.
        /// </summary>
        public void ApplyAlacrityAndClarity()
        {
            var defender = OBJECT_SELF;
            var item = GetSpellCastItem();
            var itemType = GetBaseItemType(item);

            if (_itemService.ShieldBaseItemTypes.Contains(itemType))
            {
                if (_random.D100(1) <= 10)
                {
                    if (_perkService.GetPerkLevel(defender, PerkType.Alacrity) > 0)
                    {
                        _statService.RestoreStamina(defender, 4);
                    }
                    else if (_perkService.GetPerkLevel(defender, PerkType.Clarity) > 0)
                    {
                        _statService.RestoreFP(defender, 4);
                    }
                }
            }
        }

        /// <summary>
        /// When a weapon hits, process Force Link for Beast Force perks.
        /// </summary>
        public void OnForceLinkHit()
        {
            var beast = OBJECT_SELF;
            var item = GetSpellCastItem();

            if (!_beastMastery.IsPlayerBeast(beast) || GetResRef(item) != _beastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = _perkService.GetPerkLevel(beast, PerkType.ForceLink) * 10;

                if (_random.D100(1) <= chance)
                {
                    _statService.RestoreFP(player, 1);
                }
            }
        }

        /// <summary>
        /// When a weapon hits, process Endurance Link for Beast Bruiser perks.
        /// </summary>
        public void OnEnduranceLinkHit()
        {
            var beast = OBJECT_SELF;
            var item = GetSpellCastItem();

            if (!_beastMastery.IsPlayerBeast(beast) || GetResRef(item) != _beastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = _perkService.GetPerkLevel(beast, PerkType.EnduranceLink) * 10;

                if (_random.D100(1) <= chance)
                {
                    _statService.RestoreStamina(player, 1);
                }
            }
        }
    }
}
