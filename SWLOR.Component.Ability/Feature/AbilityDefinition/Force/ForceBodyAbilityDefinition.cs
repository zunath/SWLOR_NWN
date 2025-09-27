using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceBodyAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceBodyAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceBody1(builder);
            ForceBody2(builder);

            return builder.Build();
        }

        private void ForceBody1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody1, PerkType.ForceBody)
                .Name("Force Body I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffectService.Apply(activator, activator, StatusEffectType.ForceBody1, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 2), activator, 60f);
                });
        }
        private void ForceBody2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody2, PerkType.ForceBody)
                .Name("Force Body II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffectService.Apply(activator, activator, StatusEffectType.ForceBody2, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 4), activator, 60f);
                });
        }
    }
}
