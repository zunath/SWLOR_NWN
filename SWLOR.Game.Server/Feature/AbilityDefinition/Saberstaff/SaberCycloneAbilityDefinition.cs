using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class SaberCycloneAbilityDefinition : IAbilityListDefinition
    {
        private const float PulseIntervalSeconds = 6f;
        private const float Radius = 5f;
        private const int InitialDamage = 18;
        private const int PulseDamage = 8;
        private const int FPRestorePerTarget = 1;
        private const int MaximumFPRestorePerPulse = 5;
        private const VisualEffect AreaVisualEffect = VisualEffect.Vfx_Fnf_Swinging_Blade;
        private const VisualEffect TargetVisualEffect = VisualEffect.Vfx_Com_Blood_Spark_Medium;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SaberCyclone1(builder);

            return builder.Build();
        }

        private static void SaberCyclone1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SaberCyclone1, PerkType.SaberCyclone)
                .Name("Saber Cyclone")
                .Level(1)
                .SkillType(SkillType.Saberstaff)
                .IsAreaAbility()
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasImpactAction(SaberCyclone1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void SaberCyclone1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CombatAreaPulses.ApplyCombatPulse(
                activator,
                GetLocation(activator),
                SkillType.Saberstaff,
                InitialDamage,
                Radius,
                damageType: CombatDamageType.Force,
                targetVisualEffect: TargetVisualEffect,
                areaVisualEffect: AreaVisualEffect,
                alwaysApplyAreaVisualEffect: true);

            CombatAreaPulses.SchedulePulses(
                activator,
                GetLocation(activator),
                CapstoneAbility.ActiveDurationSeconds,
                PulseIntervalSeconds,
                true,
                pulseLocation =>
                {
                    var ability = Ability.GetAbilityDetail(FeatType.SaberCyclone1);
                    Ability.BeginAbilityImpact(activator, ability);
                    CombatAreaPulses.ApplyCombatPulse(
                        activator,
                        pulseLocation,
                        SkillType.Saberstaff,
                        PulseDamage,
                        Radius,
                        damageType: CombatDamageType.Force,
                        targetVisualEffect: TargetVisualEffect,
                        areaVisualEffect: AreaVisualEffect,
                        alwaysApplyAreaVisualEffect: true);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);

                    if (summary.ImpactedTargetCount > 0)
                    {
                        var fpRestore = Math.Min(MaximumFPRestorePerPulse, summary.ImpactedTargetCount * FPRestorePerTarget);
                        Stat.RestoreFP(activator, fpRestore);
                    }
                });
        }
    }
}
