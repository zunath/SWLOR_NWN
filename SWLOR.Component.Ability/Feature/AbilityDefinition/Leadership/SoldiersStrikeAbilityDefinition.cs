using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class SoldiersStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public SoldiersStrikeAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoldiersStrike(builder);

            return builder.Build();
        }

        private void SoldiersStrike(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoldiersStrike, PerkType.SoldiersStrike)
                .Name("Soldier's Strike")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersStrike, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.SoldiersStrike);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.SoldiersStrike, false, true, false);
                });
        }
    }
}
