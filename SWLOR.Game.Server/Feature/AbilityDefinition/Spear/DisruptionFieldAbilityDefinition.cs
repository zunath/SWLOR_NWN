using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class DisruptionFieldAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float DurationSeconds = 20f;
        private const float PulseIntervalSeconds = 1f;
        private const float Radius = 5f;
        private const int FPDrainPercent = 5;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.DisruptionField1, PerkType.DisruptionField)
                .Name("Disruption Field")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.DisruptionField, 180f)
                .SkillType(SkillType.Spear)
                .IsAreaAbility()
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

            CombatAreaPulses.SchedulePulses(
                activator,
                location,
                DurationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation =>
                {
                    foreach (var hostile in CombatAreaPulses.GetHostileCreatures(activator, pulseLocation, Radius))
                    {
                        StatusEffect.ApplyStatusEffect(activator, hostile, typeof(DisruptionFieldStatusEffect), 1.2f);
                        Ability.ApplyHostileAbilityEnmity(activator, hostile);
                        var fpDrain = Math.Max(1, (int)Math.Ceiling(Stat.GetCurrentFP(hostile) * (FPDrainPercent / 100f)));
                        Stat.ReduceFP(hostile, fpDrain);
                    }
                });
        }
    }
}
