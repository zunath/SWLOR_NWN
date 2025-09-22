using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class DedicationAbilityDefinition: IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public DedicationAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Dedication(builder);

            return builder.Build();
        }

        private void Dedication(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Dedication, PerkType.Dedication)
                .Name("Dedication")
                .Level(1)
                .HasRecastDelay(RecastGroup.Dedication, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.Dedication);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.Dedication, true, true, false);
                });
        }
    }
}
