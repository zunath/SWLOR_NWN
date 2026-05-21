using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceGripAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceGrip1(builder);
            ForceGrip2(builder);
            ForceGrip3(builder);

            return builder.Build();
        }

        private static void ForceGrip1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceGrip1, PerkType.ForceGrip)
                .Name("Force Grip I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceGrip, 36f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceGrip1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceGrip2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceGrip2, PerkType.ForceGrip)
                .Name("Force Grip II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceGrip, 36f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceGrip2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void ForceGrip3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceGrip3, PerkType.ForceGrip)
                .Name("Force Grip III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceGrip, 36f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceGrip3ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceGrip3,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void ForceGrip1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                3,
                typeof(ImmobilizedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                afterSuccessfulHit: InterruptActivation);
        }

        private static void ForceGrip2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                4,
                typeof(ImmobilizedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                afterSuccessfulHit: InterruptActivation);
        }

        private static void ForceGrip3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                4,
                typeof(ImmobilizedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 2,
                afterSuccessfulHit: InterruptActivation);
        }

        private static void InterruptActivation(uint target)
        {
            AssignCommand(target, () => ClearAllActions());
        }

    }
}
