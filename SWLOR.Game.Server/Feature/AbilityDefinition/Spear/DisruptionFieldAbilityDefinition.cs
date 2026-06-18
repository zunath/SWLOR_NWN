using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class DisruptionFieldAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float DurationSeconds = 20f;
        private const float PulseIntervalSeconds = 1f;
        private const float Radius = 5f;
        private const int FPDrainPercent = 5;
        private const VisualEffect DisruptionFieldMarkerVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Black;
        private const VisualEffect DisruptionFieldPulseVisualEffect = VisualEffect.Vfx_Fnf_Gas_Explosion_Mind;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.DisruptionField1, PerkType.DisruptionField)
                .Name("Disruption Field")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.DisruptionField, 180f)
                .SkillType(SkillType.Spear)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.DisruptionField1,
                    Radius,
                    AbilityTargetingFlags.HarmsEnemies)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectVisualEffect(DisruptionFieldMarkerVisualEffect, false, 2f),
                location,
                DurationSeconds);

            CombatAreaPulses.SchedulePulses(
                activator,
                location,
                DurationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation =>
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(DisruptionFieldPulseVisualEffect), pulseLocation);

                    foreach (var hostile in CombatAreaPulses.GetHostileCreatures(activator, pulseLocation, Radius))
                    {
                        StatusEffect.ApplyStatusEffect(activator, hostile, typeof(DisruptionFieldStatusEffect), 1.2f);
                        Combat.ApplySpearDisablerSuppressionRiders(activator, hostile);
                        Ability.ApplyHostileAbilityEnmity(activator, hostile);
                        var fpDrain = Math.Max(1, (int)Math.Ceiling(Stat.GetCurrentFP(hostile) * (FPDrainPercent / 100f)));
                        Stat.ReduceFP(hostile, fpDrain);
                    }
                });
        }
    }
}
