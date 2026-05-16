using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class SaberCycloneAbilityDefinition : IAbilityListDefinition
    {
        private const float ChannelDurationSeconds = 6f;
        private const float PulseIntervalSeconds = 2f;
        private const float Radius = 5f;
        private const int FPRestorePerTarget = 3;

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
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .HasImpactAction(SaberCyclone1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void SaberCyclone1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CombatAreaPulses.SchedulePulses(
                activator,
                GetLocation(activator),
                ChannelDurationSeconds,
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
                        25,
                        Radius);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);

                    if (summary.ImpactedTargetCount > 0)
                        Stat.RestoreFP(activator, summary.ImpactedTargetCount * FPRestorePerTarget);
                });
        }
    }
}
