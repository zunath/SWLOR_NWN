using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;


using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class SoldiersPrecisionAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public SoldiersPrecisionAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoldiersPrecision(builder);

            return builder.Build();
        }

        private void SoldiersPrecision(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoldiersPrecision, PerkType.SoldiersPrecision)
                .Name("Soldier's Precision")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersPrecision, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.SoldiersPrecision);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.SoldiersPrecision, false, true, false);
                });
        }
    }
}
