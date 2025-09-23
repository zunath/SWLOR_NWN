using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class SoldiersSpeedAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityService _abilityService;

        public SoldiersSpeedAbilityDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoldiersSpeed(builder);

            return builder.Build();
        }

        private void SoldiersSpeed(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoldiersSpeed, PerkType.SoldiersSpeed)
                .Name("Soldier's Speed")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersSpeed, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.SoldiersSpeed);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.SoldiersSpeed, false, true, false);
                });
        }
    }
}
