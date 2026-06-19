using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class SuppressiveLineAbilityDefinition : IAbilityListDefinition
    {
        private const float LineLength = 20f;
        private const float LineWidth = 3f;
        private const float ChannelDurationSeconds = 6f;
        private const float PulseIntervalSeconds = 2f;
        private const float ChannelMonitorIntervalSeconds = 0.5f;
        private const float ChannelMoveToleranceMeters = 0.5f;
        private const int PulseDamage = 6;
        private const int DisorientedDurationSeconds = 4;
        private const string ChannelIdLocalName = "SUPPRESSIVE_LINE_CHANNEL_ID";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SuppressiveLine1(builder);

            return builder.Build();
        }

        private static void SuppressiveLine1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SuppressiveLine1, PerkType.SuppressiveLine)
                .Name("Suppressive Line")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SuppressiveLine, 60f)
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(LineLength)
                .HasTargetingLine(
                    Spell.SuppressiveLine1,
                    LineLength,
                    LineWidth,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsAreaAbility()
                .HasImpactAction(SuppressiveLine1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void SuppressiveLine1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var originLocation = GetLocation(activator);
            var destinationLocation = ResolveLineDestination(activator, target, targetLocation);
            var channelId = Guid.NewGuid().ToString();

            SetLocalString(activator, ChannelIdLocalName, channelId);
            Activity.SetBusy(activator, ActivityStatusType.AbilityActivation);

            ApplySuppressiveLinePulse(activator, destinationLocation);
            ScheduleChannelMonitor(activator, channelId, originLocation, ChannelMonitorIntervalSeconds);
            DelayCommand(0.15f, () =>
            {
                if (IsChannelActive(activator, channelId))
                    AssignCommand(activator, () => ClearAllActions());
            });

            CombatAreaPulses.SchedulePulses(
                activator,
                originLocation,
                ChannelDurationSeconds,
                PulseIntervalSeconds,
                false,
                (_, elapsed) =>
                {
                    if (!CanContinueChannel(activator, channelId, originLocation))
                    {
                        EndChannel(activator, channelId);
                        return;
                    }

                    var ability = Ability.GetAbilityDetail(FeatType.SuppressiveLine1);
                    Ability.BeginAbilityImpact(activator, ability);
                    ApplySuppressiveLinePulse(activator, destinationLocation);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);

                    if (elapsed >= ChannelDurationSeconds - 0.01f)
                        EndChannel(activator, channelId);
                });
        }

        private static Location ResolveLineDestination(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetLocation(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

        private static void ApplySuppressiveLinePulse(uint activator, Location destinationLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                OBJECT_INVALID,
                destinationLocation,
                SkillType.Rifle,
                PulseDamage,
                DisorientedDurationSeconds,
                typeof(DisorientedStatusEffect),
                CombatImpactAreaShape.Line,
                0f,
                LineLength,
                LineWidth,
                impactAnimation: Animation.PointPistol);
        }

        private static void ScheduleChannelMonitor(uint activator, string channelId, Location originLocation, float elapsed)
        {
            DelayCommand(ChannelMonitorIntervalSeconds, () =>
            {
                if (!IsChannelActive(activator, channelId))
                    return;

                if (!CanContinueChannel(activator, channelId, originLocation) ||
                    elapsed >= ChannelDurationSeconds)
                {
                    EndChannel(activator, channelId);
                    return;
                }

                ScheduleChannelMonitor(
                    activator,
                    channelId,
                    originLocation,
                    elapsed + ChannelMonitorIntervalSeconds);
            });
        }

        private static bool CanContinueChannel(uint activator, string channelId, Location originLocation)
        {
            if (!IsChannelActive(activator, channelId) ||
                GetCurrentHitPoints(activator) <= 0 ||
                !GetIsObjectValid(GetAreaFromLocation(originLocation)) ||
                GetArea(activator) != GetAreaFromLocation(originLocation))
            {
                return false;
            }

            return GetDistanceBetweenLocations(GetLocation(activator), originLocation) <= ChannelMoveToleranceMeters;
        }

        private static bool IsChannelActive(uint activator, string channelId)
        {
            return GetIsObjectValid(activator) &&
                   GetLocalString(activator, ChannelIdLocalName) == channelId;
        }

        private static void EndChannel(uint activator, string channelId)
        {
            if (!IsChannelActive(activator, channelId))
                return;

            DeleteLocalString(activator, ChannelIdLocalName);

            if (Activity.GetBusyType(activator) == ActivityStatusType.AbilityActivation)
                Activity.ClearBusy(activator);
        }
    }
}
