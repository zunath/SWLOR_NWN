using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class SoldiersStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SoldiersStrikeAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

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
                .HasRecastDelay(RecastGroupType.SoldiersStrike, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return AbilityService.ToggleAura(activator, StatusEffectType.SoldiersStrike);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    AbilityService.ApplyAura(activator, StatusEffectType.SoldiersStrike, false, true, false);
                });
        }
    }
}
