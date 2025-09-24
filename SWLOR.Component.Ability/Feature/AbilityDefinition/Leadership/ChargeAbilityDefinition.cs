using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class ChargeAbilityDefinition: IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public ChargeAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Charge(builder);

            return builder.Build();
        }

        private void Charge(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Charge, PerkType.Charge)
                .Name("Charge")
                .Level(1)
                .HasRecastDelay(RecastGroup.Charge, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.Charge);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.Charge, true, true, false);
                });
        }
    }
}
