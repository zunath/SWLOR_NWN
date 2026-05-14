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
    public sealed class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);

            return builder.Build();
        }

        private static void ForcePush1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ForcePush1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(2);
        }

        private static void ForcePush2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(ForcePush2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void ForcePush3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(ForcePush3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForcePush1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                2,
                typeof(KnockdownStatusEffect),
                false,
                new[] { typeof(HobbleStatusEffect) },
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForcePush2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                2,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Line,
                0f,
                8f,
                2.5f,
                new[] { typeof(HobbleStatusEffect) },
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 2);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForcePush3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                2,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                new[] { typeof(HobbleStatusEffect) },
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 3);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

    }
}
