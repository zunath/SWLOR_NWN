using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class SaturationTossAbilityDefinition : IAbilityListDefinition
    {
        private const float FieldDurationSeconds = 12f;
        private const float PulseIntervalSeconds = 4f;
        private const float FieldRadius = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SaturationToss1(builder);

            return builder.Build();
        }

        private static void SaturationToss1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SaturationToss1, PerkType.SaturationToss)
                .Name("Saturation Toss")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SaturationToss, 120f)
                .SkillType(SkillType.Throwing)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(SaturationToss1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SaturationToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CombatAreaPulses.SchedulePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                FieldDurationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation =>
                {
                    var ability = Ability.GetAbilityDetail(FeatType.SaturationToss1);
                    Ability.BeginAbilityImpact(activator, ability);
                    CombatAreaPulses.ApplyCombatPulse(
                        activator,
                        pulseLocation,
                        SkillType.Throwing,
                        10,
                        FieldRadius);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);
                });
        }
    }
}
