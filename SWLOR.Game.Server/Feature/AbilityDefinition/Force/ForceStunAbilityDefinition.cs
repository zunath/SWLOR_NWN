using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceStunAbilityDefinition : IAbilityListDefinition
    {
        private const float AOESize = RadiusSize.Medium;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private static void Impact(uint source, uint target)
        {
            const int DC = 12;
            const string EffectTag = "StatusEffectType.ForceStun";
            var checkResult = WillSave(target, DC, SavingThrowType.None, source);

            if (checkResult == SavingThrowResultType.Failed)
            {
                var effect = EffectDazed();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);
            }
            else if(checkResult == SavingThrowResultType.Success)
            {
                var effect = EffectAccuracyDecrease(2);
                effect = EffectLinkEffects(effect, EffectACDecrease(2));
                effect = TagEffect(effect, EffectTag);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);
            }

            CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);

            Enmity.ModifyEnmity(source, target, 850);
        }
        
        private static void ForceStun1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun1, PerkType.ForceStun)
                .Name("Force Stun I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);
                });
        }

        private static void ForceStun2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun2, PerkType.ForceStun)
                .Name("Force Stun II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, activator))
                        {
                            // Apply to nearest other creature, then exit loop. 
                            Impact(activator, targetCreature);
                            break;
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                    }
                });
        }

        private static void ForceStun3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun3, PerkType.ForceStun)
                .Name("Force Stun III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(8)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target);

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, activator))
                        {
                            // Apply to nearest other creature, then move on to the next.
                            // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                            Impact(activator, targetCreature);
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                    }
                });
        }
    }
}