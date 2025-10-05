using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.Force
{
    public class ForceBodyAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceBodyAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies

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
                .HasRecastDelay(RecastGroupType.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 2), activator, 60f);
                });
        }
        private void ForceBody2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody2, PerkType.ForceBody)
                .Name("Force Body II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 4), activator, 60f);
                });
        }
    }
}
