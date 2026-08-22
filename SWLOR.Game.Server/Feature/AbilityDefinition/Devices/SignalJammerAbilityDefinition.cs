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
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class SignalJammerAbilityDefinition : IAbilityListDefinition
    {
        private const float RadiusMeters = 5f;
        private const float DurationSeconds = 45f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.SignalJammer1, PerkType.SignalJammer)
                .Name("Signal Jammer")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.SignalJammer, 24f)
                .SkillType(SkillType.Devices)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.SignalJammer1,
                    RadiusMeters,
                    AbilityTargetingFlags.HarmsEnemies)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                0,
                3,
                typeof(SignalJammerStatusEffect),
                RadiusMeters,
                DurationSeconds,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Imp_Pulse_Negative,
                markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue,
                markerVisualEffectScale: 2f);
        }
    }
}
