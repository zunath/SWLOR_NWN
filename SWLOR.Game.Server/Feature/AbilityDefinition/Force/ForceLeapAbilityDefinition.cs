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
    public sealed class ForceLeapAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceLeap1(builder);
            ForceLeap2(builder);

            return builder.Build();
        }

        private static void ForceLeap1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLeap1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void ForceLeap2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLeap2, PerkType.ForceLeap)
                .Name("Force Leap II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceLeap, 30f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(18f)
                .RequiresTarget()
                .HasImpactAction(ForceLeap2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceLeap1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                10,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForceLeap2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                18,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void LeapAndInterrupt(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            AssignCommand(target, () => ClearAllActions());
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_1), activator);
            AssignCommand(activator, () => ActionJumpToObject(target));
        }
    }
}
