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
    public class RejuvenationAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public RejuvenationAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

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
                .HasRecastDelay(RecastGroupType.Rejuvenation, 180f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return AbilityService.ToggleAura(activator, StatusEffectType.Rejuvenation);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    AbilityService.ApplyAura(activator, StatusEffectType.Rejuvenation, false, true, false);
                });
        }
    }
}
