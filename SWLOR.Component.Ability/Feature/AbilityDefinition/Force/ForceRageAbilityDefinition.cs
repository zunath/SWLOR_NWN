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
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceRageAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceRageAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceRage1(builder);
            ForceRage2(builder);

            return builder.Build();
        }

        private void ForceRage1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceRage1, PerkType.ForceRage)
                .Name("Force Rage I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceRage, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = StatusEffectService;
                    var combatPointService = CombatPointService;
                    var enmityService = EnmityService;

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceRage1, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Negative_Energy), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }

        private void ForceRage2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceRage2, PerkType.ForceRage)
                .Name("Force Rage II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceRage, 30f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = StatusEffectService;
                    var combatPointService = CombatPointService;
                    var enmityService = EnmityService;

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceRage2, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Negative_Energy), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }
    }
}
