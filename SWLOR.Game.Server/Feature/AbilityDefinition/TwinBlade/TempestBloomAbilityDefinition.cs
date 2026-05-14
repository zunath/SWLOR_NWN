using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class TempestBloomAbilityDefinition : IAbilityListDefinition
    {
        private const float ChannelDurationSeconds = 6f;
        private const float PulseIntervalSeconds = 2f;
        private const float Radius = 5f;

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
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .HasImpactAction(TempestBloom1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void TempestBloom1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CombatAreaPulses.SchedulePulses(
                activator,
                GetLocation(activator),
                ChannelDurationSeconds,
                PulseIntervalSeconds,
                true,
                (pulseLocation, elapsed) =>
                {
                    CombatAreaPulses.ApplyCombatPulse(
                        activator,
                        pulseLocation,
                        SkillType.TwinBlade,
                        20,
                        Radius,
                        elapsed >= ChannelDurationSeconds - 0.01f ? typeof(KnockdownStatusEffect) : null,
                        elapsed >= ChannelDurationSeconds - 0.01f ? 3 : 0);
                });
        }
    }
}
