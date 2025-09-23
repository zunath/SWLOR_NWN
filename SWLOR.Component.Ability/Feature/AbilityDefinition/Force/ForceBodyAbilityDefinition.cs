using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceBodyAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public ForceBodyAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

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
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, activator, StatusEffectType.ForceBody1, 60f);
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
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    _statusEffectService.Apply(activator, activator, StatusEffectType.ForceBody2, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 4), activator, 60f);
                });
        }
    }
}
