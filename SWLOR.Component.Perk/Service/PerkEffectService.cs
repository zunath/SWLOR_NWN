using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Perk.Service
{
    /// <summary>
    /// Service for handling perk-specific effects and behaviors.
    /// </summary>
    public class PerkEffectService : IPerkEffectService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();

        public PerkEffectService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// When a weapon hits, apply Alacrity and Clarity effects for OneHanded perks.
        /// </summary>
        public void ApplyAlacrityAndClarity()
        {
            var defender = OBJECT_SELF;
            var item = GetSpellCastItem();
            var itemType = GetBaseItemType(item);

            if (ItemService.ShieldBaseItemTypes.Contains(itemType))
            {
                if (Random.D100(1) <= 10)
                {
                    if (PerkService.GetPerkLevel(defender, PerkType.Alacrity) > 0)
                    {
                        StatService.RestoreStamina(defender, 4);
                    }
                    else if (PerkService.GetPerkLevel(defender, PerkType.Clarity) > 0)
                    {
                        StatService.RestoreFP(defender, 4);
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

            if (!BeastMastery.IsPlayerBeast(beast) || GetResRef(item) != BeastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = PerkService.GetPerkLevel(beast, PerkType.ForceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    StatService.RestoreFP(player, 1);
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

            if (!BeastMastery.IsPlayerBeast(beast) || GetResRef(item) != BeastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = PerkService.GetPerkLevel(beast, PerkType.EnduranceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    StatService.RestoreStamina(player, 1);
                }
            }
        }
    }
}
