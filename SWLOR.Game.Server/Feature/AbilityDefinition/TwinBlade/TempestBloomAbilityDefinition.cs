using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class TempestBloomAbilityDefinition : IAbilityListDefinition
    {
        private const float PulseIntervalSeconds = 6f;
        private const float Radius = 5f;
        private const int InitialDamage = 20;
        private const int PulseDamage = 8;
        private const int MaximumMarkStacks = 3;
        private const VisualEffect AreaVisualEffect = VisualEffect.Vfx_Fnf_Swinging_Blade;
        private const VisualEffect TargetVisualEffect = VisualEffect.Vfx_Com_Blood_Spark_Medium;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TempestBloom1(builder);

            return builder.Build();
        }

        private static void TempestBloom1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TempestBloom1, PerkType.TempestBloom)
                .Name("Tempest Bloom")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasImpactAction(TempestBloom1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void TempestBloom1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CombatAreaPulses.ApplyCombatPulse(
                activator,
                GetLocation(activator),
                SkillType.TwinBlade,
                InitialDamage,
                Radius,
                targetVisualEffect: TargetVisualEffect,
                areaVisualEffect: AreaVisualEffect,
                alwaysApplyAreaVisualEffect: true,
                afterSuccessfulHit: ApplyTempestMark);

            CombatAreaPulses.SchedulePulses(
                activator,
                GetLocation(activator),
                CapstoneAbility.ActiveDurationSeconds,
                PulseIntervalSeconds,
                true,
                pulseLocation =>
                {
                    var ability = Ability.GetAbilityDetail(FeatType.TempestBloom1);
                    Ability.BeginAbilityImpact(activator, ability);
                    CombatAreaPulses.ApplyCombatPulse(
                        activator,
                        pulseLocation,
                        SkillType.TwinBlade,
                        PulseDamage,
                        Radius,
                        targetVisualEffect: TargetVisualEffect,
                        areaVisualEffect: AreaVisualEffect,
                        alwaysApplyAreaVisualEffect: true,
                        afterSuccessfulHit: ApplyTempestMark);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);
                });

            void ApplyTempestMark(uint affectedEnemy)
            {
                var activeStacks = StatusEffect.GetCreatureStatusEffects(affectedEnemy)
                    .GetAllEffects()
                    .Count(effect => effect.GetType() == typeof(TempestMarkStatusEffect) && effect.Source == activator);

                if (activeStacks >= MaximumMarkStacks)
                    return;

                StatusEffect.ApplyStatusEffect(
                    activator,
                    affectedEnemy,
                    typeof(TempestMarkStatusEffect),
                    CapstoneAbility.ActiveDurationSeconds,
                    CombatDamageType.Physical);
            }
        }
    }
}
