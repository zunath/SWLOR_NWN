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
    public sealed class WeakenResolveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WeakenResolve1(builder);
            WeakenResolve2(builder);

            return builder.Build();
        }

        private static void WeakenResolve1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WeakenResolve1, PerkType.WeakenResolve)
                .Name("Weaken Resolve I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WeakenResolve, 12f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_mind")
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(WeakenResolve1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void WeakenResolve2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WeakenResolve2, PerkType.WeakenResolve)
                .Name("Weaken Resolve II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WeakenResolve, 12f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_mind")
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(WeakenResolve2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void WeakenResolve1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                24,
                typeof(WeakenResolve1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                afterSuccessfulHit: ApplyResolveDebuffVisual);
            LightGuardianPowerSupport.ApplyCourageousResolve(activator);
        }

        private static void WeakenResolve2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                24,
                typeof(WeakenResolve2StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                afterSuccessfulHit: ApplyResolveDebuffVisual);
            LightGuardianPowerSupport.ApplyCourageousResolve(activator);
        }

        private static void ApplyResolveDebuffVisual(uint target)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black), target, 1.0f);
        }

    }
}
