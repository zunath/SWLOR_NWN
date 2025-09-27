using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceStunAbilityDefinition : IAbilityListDefinition
    {
        private const float AOESize = RadiusSize.Medium;
        private readonly IServiceProvider _serviceProvider;

        public ForceStunAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private void Impact(uint source, uint target)
        {
            var dc = CombatService.CalculateSavingThrowDC(source, SavingThrowCategoryType.Will, 12);
            const string EffectTag = "StatusEffectType.ForceStun";
            var checkResult = WillSave(target, dc, SavingThrowType.None, source);
            const float Duration = 6.1f;

            if (checkResult == SavingThrowResultType.Failed)
            {
                var effect = EffectDazed();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffectType.Vfx_Dur_Iounstone_Blue));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);

                AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Dazed);
            }
            else if(checkResult == SavingThrowResultType.Success)
            {
                var effect = EffectAccuracyDecrease(2);
                effect = EffectLinkEffects(effect, EffectACDecrease(2));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);
            }

            CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);

            EnmityService.ModifyEnmity(source, target, 850);
        }
        
        private void ForceStun1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun1, PerkType.ForceStun)
                .Name("Force Stun I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);
                });
        }

        private void ForceStun2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun2, PerkType.ForceStun)
                .Name("Force Stun II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(ShapeType.Sphere, AOESize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, activator))
                        {
                            // Apply to nearest other creature, then exit loop. 
                            Impact(activator, targetCreature);
                            break;
                        }
                        targetCreature = GetNextObjectInShape(ShapeType.Sphere, AOESize, GetLocation(target), true);
                    }
                });
        }

        private void ForceStun3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun3, PerkType.ForceStun)
                .Name("Force Stun III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(8)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(ShapeType.Sphere, AOESize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, activator))
                        {
                            // Apply to nearest other creature, then move on to the next.
                            // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                            Impact(activator, targetCreature);
                        }
                        targetCreature = GetNextObjectInShape(ShapeType.Sphere, AOESize, GetLocation(target), true);
                    }
                });
        }
    }
}
