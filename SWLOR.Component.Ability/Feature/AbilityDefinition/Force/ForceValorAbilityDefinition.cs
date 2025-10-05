using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceValorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceValorAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceValor1(builder);
            ForceValor2(builder);

            return builder.Build();
        }

        private void ForceValor1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceValor1, PerkType.ForceValor)
                .Name("Force Valor I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceValor, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Ac_Bonus), target);

                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    EnmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }

        private void ForceValor2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceValor2, PerkType.ForceValor)
                .Name("Force Valor II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceValor, 30f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Ac_Bonus), target);

                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    EnmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }
    }
}
