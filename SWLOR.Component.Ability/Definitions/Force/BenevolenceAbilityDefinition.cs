using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Force
{
    public class BenevolenceAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private const string BeneRegen = "FORCE_BENEVOLENCE";

        public BenevolenceAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICharacterResourceService CharacterResourceService => _serviceProvider.GetRequiredService<ICharacterResourceService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Benevolence1(builder);
            Benevolence2(builder);
            Benevolence3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            var targetBonus = willBonus;
            if (target != activator && CharacterResourceService.GetCurrentFP(activator) >= 16)
            {
                RemoveEffectByTag(target, BeneRegen);

                var willRestore = (willBonus / 2) * 4;
                var duration = 90f + (willBonus * 60f);
                var effect = EffectRegenerate(willRestore, 24f);
                StatService.ReduceFP(activator, 10);
                StatService.ReduceStamina(activator, willRestore);
                StatService.RestoreFP(target, willRestore);
                StatService.RestoreStamina(target, willRestore);
                targetBonus = willBonus * 4;

                effect = TagEffect(effect, BeneRegen);
                ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
            }
            var willHeal = baseAmount + (targetBonus * 4) + Random.D4(targetBonus);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(willHeal), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Healing_M), target);

            EnmityService.ModifyEnmityOnAll(activator, 150 + (willHeal / 4));
            CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private void Benevolence1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Benevolence1, PerkType.Benevolence)
                .Name("Benevolence I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void Benevolence2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Benevolence2, PerkType.Benevolence)
                .Name("Benevolence II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 60);
                });
        }

        private void Benevolence3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Benevolence3, PerkType.Benevolence)
                .Name("Benevolence III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 90);
                });
        }
    }
}
