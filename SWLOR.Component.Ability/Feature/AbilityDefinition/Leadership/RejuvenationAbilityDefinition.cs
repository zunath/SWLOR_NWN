using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class RejuvenationAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public RejuvenationAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Rejuvenation(builder);

            return builder.Build();
        }

        private void Rejuvenation(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Rejuvenation, PerkType.Rejuvenation)
                .Name("Rejuvenation")
                .Level(1)
                .HasRecastDelay(RecastGroup.Rejuvenation, 180f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.Rejuvenation);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.Rejuvenation, false, true, false);
                });
        }
    }
}
