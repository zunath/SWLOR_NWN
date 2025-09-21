using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class ChargeAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly IAbilityService _abilityService;

        public ChargeAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Charge();

            return _builder.Build();
        }

        private void Charge()
        {
            _builder.Create(FeatType.Charge, PerkType.Charge)
                .Name("Charge")
                .Level(1)
                .HasRecastDelay(RecastGroup.Charge, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return AbilityService.ToggleAura(activator, StatusEffectType.Charge);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    AbilityService.ApplyAura(activator, StatusEffectType.Charge, true, true, false);
                });
        }
    }
}
