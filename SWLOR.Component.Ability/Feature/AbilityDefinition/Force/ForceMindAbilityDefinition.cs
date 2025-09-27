using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceMindAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceMindAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceMind1(builder);
            ForceMind2(builder);

            return builder.Build();
        }

        private void ForceMind1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind1, PerkType.ForceMind)
                .Name("Force Mind I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffectService.Apply(activator, activator, StatusEffectType.ForceMind1, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 2), activator, 60f);
                });
        }
        private void ForceMind2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind2, PerkType.ForceMind)
                .Name("Force Mind II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffectService.Apply(activator, activator, StatusEffectType.ForceMind2, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 4), activator, 60f);
                });
        }
    }
}
