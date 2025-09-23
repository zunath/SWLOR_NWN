using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class FrenziedShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public FrenziedShoutAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            FrenziedShout(builder);

            return builder.Build();
        }

        private void FrenziedShout(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FrenziedShout, PerkType.FrenziedShout)
                .Name("Frenzied Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.FrenziedShout, 120f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.FrenziedShout);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.FrenziedShout, false, false, true);
                });
        }
    }
}
