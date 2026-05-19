using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class DeviceAbilityEffects
    {
        private const float FieldEngineerPulseIntervalSeconds = 3f;
        private const float FieldEngineerVisualRefreshPaddingSeconds = 0.2f;
        private const float FieldEngineerVisualMinimumDurationSeconds = 0.1f;
        private const string FieldEngineerPulseMarkerResref = "_mdrn_pl_emitter";
        private const string FieldEngineerPulseMarkerTag = "field_engineer_pulse_marker";

        private static readonly Dictionary<uint, List<FieldEngineerPulseEmitter>> _activeFieldEngineerPulseEmitters = new();

        private sealed class FieldEngineerPulseEmitter
        {
            public FieldEngineerPulseEmitter(
                uint activator,
                Location location,
                SkillType skillType,
                int baseDamage,
                int statusDuration,
                Type statusEffect,
                float radius,
                float durationSeconds,
                CombatDamageType damageType,
                VisualEffect targetVisualEffect,
                VisualEffect areaVisualEffect,
                VisualEffect markerVisualEffect,
                float markerVisualEffectScale,
                bool isAreaPulse)
            {
                Activator = activator;
                Location = location;
                SkillType = skillType;
                BaseDamage = baseDamage;
                StatusDuration = statusDuration;
                StatusEffect = statusEffect;
                Radius = radius;
                RemainingSeconds = durationSeconds;
                DamageType = damageType;
                TargetVisualEffect = targetVisualEffect;
                AreaVisualEffect = areaVisualEffect;
                MarkerVisualEffect = markerVisualEffect;
                MarkerVisualEffectScale = markerVisualEffectScale;
                IsAreaPulse = isAreaPulse;
                MarkerObject = OBJECT_INVALID;
            }

            public uint Activator { get; }
            public Location Location { get; }
            public SkillType SkillType { get; }
            public int BaseDamage { get; }
            public int StatusDuration { get; }
            public Type StatusEffect { get; }
            public float Radius { get; }
            public float RemainingSeconds { get; set; }
            public CombatDamageType DamageType { get; }
            public VisualEffect TargetVisualEffect { get; }
            public VisualEffect AreaVisualEffect { get; }
            public VisualEffect MarkerVisualEffect { get; }
            public float MarkerVisualEffectScale { get; }
            public bool IsAreaPulse { get; }
            public uint MarkerObject { get; set; }
        }

        public static int ApplyCapacitorRigBonus(uint activator, int amount)
        {
            var bonusPercent = Stat.GetStatAdjustment(activator, StatType.DeviceShieldTemporaryHPPercentAdjustment);
            return amount + amount * bonusPercent / 100;
        }

        public static float ApplyCapacitorRigDurationBonus(uint activator, float durationSeconds)
        {
            return durationSeconds + Stat.GetStatAdjustment(activator, StatType.DeviceShieldDurationBonusSeconds);
        }

        public static float ApplyGrenadeRadiusBonus(uint activator, float baseRadius)
        {
            return baseRadius + Stat.GetStatAdjustment(activator, StatType.GrenadeRadiusBonusTenths) / 10f;
        }

        public static int ApplyGrenadeControlPotencyBonus(uint activator, int baseAdjustment)
        {
            var bonus = Stat.GetStatAdjustment(activator, StatType.GrenadeControlPotencyBonus);
            if (baseAdjustment == 0 || bonus == 0)
                return baseAdjustment;

            return baseAdjustment < 0
                ? baseAdjustment - bonus
                : baseAdjustment + bonus;
        }

        public static int GetAssaultGadgetAccuracyAdjustment(uint activator)
        {
            return Stat.GetStatAdjustment(activator, StatType.AssaultGadgetAccuracyPercentAdjustment);
        }

        public static int GetAssaultGadgetCriticalRateAdjustment(uint activator)
        {
            return Stat.GetStatAdjustment(activator, StatType.AssaultGadgetCriticalRatePercentAdjustment);
        }

        public static Func<uint, int> GetAssaultGadgetDamageAdjustment(uint activator)
        {
            var adjustment = Stat.GetStatAdjustment(activator, StatType.AssaultGadgetDamagePercentAdjustment);
            return adjustment == 0
                ? null
                : _ => adjustment;
        }

        public static void ScheduleSingleHostilePulses(
            uint activator,
            Location location,
            SkillType skillType,
            int baseDamage,
            int statusDuration,
            Type statusEffect,
            float radius,
            float durationSeconds,
            CombatDamageType damageType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect = VisualEffect.None,
            VisualEffect markerVisualEffect = VisualEffect.None,
            float markerVisualEffectScale = 1f)
        {
            TrackFieldEngineerPulseEmitter(new FieldEngineerPulseEmitter(
                activator,
                location,
                skillType,
                baseDamage,
                statusDuration,
                statusEffect,
                radius,
                durationSeconds,
                damageType,
                targetVisualEffect,
                areaVisualEffect,
                markerVisualEffect,
                markerVisualEffectScale,
                false));
        }

        public static void ScheduleAreaHostilePulses(
            uint activator,
            Location location,
            SkillType skillType,
            int baseDamage,
            int statusDuration,
            Type statusEffect,
            float radius,
            float durationSeconds,
            CombatDamageType damageType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect = VisualEffect.None,
            VisualEffect markerVisualEffect = VisualEffect.None,
            float markerVisualEffectScale = 1f)
        {
            TrackFieldEngineerPulseEmitter(new FieldEngineerPulseEmitter(
                activator,
                location,
                skillType,
                baseDamage,
                statusDuration,
                statusEffect,
                radius,
                durationSeconds,
                damageType,
                targetVisualEffect,
                areaVisualEffect,
                markerVisualEffect,
                markerVisualEffectScale,
                true));
        }

        public static bool ExtendActiveFieldEngineerPulses(uint activator, float seconds)
        {
            if (seconds <= 0f ||
                !_activeFieldEngineerPulseEmitters.TryGetValue(activator, out var emitters))
            {
                return false;
            }

            var extendedAny = false;
            foreach (var emitter in emitters.ToArray())
            {
                if (!IsFieldEngineerEmitterValid(emitter))
                {
                    RemoveFieldEngineerPulseEmitter(emitter);
                    continue;
                }

                emitter.RemainingSeconds += seconds;
                extendedAny = true;
            }

            return extendedAny;
        }

        private static void TrackFieldEngineerPulseEmitter(FieldEngineerPulseEmitter emitter)
        {
            if (!_activeFieldEngineerPulseEmitters.TryGetValue(emitter.Activator, out var emitters))
            {
                emitters = new List<FieldEngineerPulseEmitter>();
                _activeFieldEngineerPulseEmitters[emitter.Activator] = emitters;
            }

            emitters.Add(emitter);
            EnsureFieldEngineerPulseEmitterMarker(emitter);
            ApplyFieldEngineerPulseEmitterVisual(emitter);
            ScheduleNextFieldEngineerPulse(emitter);
        }

        private static void ScheduleNextFieldEngineerPulse(FieldEngineerPulseEmitter emitter)
        {
            DelayCommand(FieldEngineerPulseIntervalSeconds, () =>
            {
                if (!IsFieldEngineerEmitterTracked(emitter))
                    return;

                if (!IsFieldEngineerEmitterValid(emitter))
                {
                    RemoveFieldEngineerPulseEmitter(emitter);
                    return;
                }

                emitter.RemainingSeconds -= FieldEngineerPulseIntervalSeconds;
                if (emitter.RemainingSeconds < -0.01f)
                {
                    RemoveFieldEngineerPulseEmitter(emitter);
                    return;
                }

                ApplyFieldEngineerPulseEmitterVisual(emitter);

                if (emitter.IsAreaPulse)
                    ApplyAreaHostilePulse(emitter);
                else
                    ApplySingleHostilePulse(emitter);

                if (emitter.RemainingSeconds > 0.01f)
                    ScheduleNextFieldEngineerPulse(emitter);
                else
                    RemoveFieldEngineerPulseEmitter(emitter);
            });
        }

        private static void ApplySingleHostilePulse(FieldEngineerPulseEmitter emitter)
        {
            var radius = emitter.Radius + Stat.GetStatAdjustment(emitter.Activator, StatType.BeaconPulseRangeBonusMeters);
            var target = GetNearestHostileCreature(emitter.Activator, emitter.Location, radius);
            if (!GetIsObjectValid(target))
                return;

            if (emitter.AreaVisualEffect != VisualEffect.None)
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(emitter.AreaVisualEffect), emitter.Location);

            var damageBonus = Stat.GetStatAdjustment(emitter.Activator, StatType.BeaconPulseDamagePercentAdjustment);
            var damagePercentAdjustment = damageBonus == 0
                ? null
                : new Func<uint, int>(_ => damageBonus);

            Ability.ApplyCombatImpact(
                emitter.Activator,
                target,
                emitter.Location,
                emitter.SkillType,
                emitter.BaseDamage,
                emitter.StatusDuration,
                emitter.StatusEffect,
                false,
                Array.Empty<Type>(),
                damageType: emitter.DamageType,
                targetVisualEffect: emitter.TargetVisualEffect,
                damagePercentAdjustment: damagePercentAdjustment,
                hitChancePercentAdjustment: Stat.GetStatAdjustment(emitter.Activator, StatType.BeaconPulseAccuracyPercentAdjustment),
                criticalRatePercentAdjustment: Stat.GetStatAdjustment(emitter.Activator, StatType.BeaconPulseCriticalRatePercentAdjustment),
                playImpactAnimation: false);
        }

        private static void ApplyAreaHostilePulse(FieldEngineerPulseEmitter emitter)
        {
            Ability.ApplyTelegraphedCombatImpact(
                emitter.Activator,
                OBJECT_INVALID,
                emitter.Location,
                emitter.SkillType,
                emitter.BaseDamage,
                emitter.StatusDuration,
                emitter.StatusEffect,
                CombatImpactAreaShape.Sphere,
                0f,
                emitter.Radius,
                0f,
                Array.Empty<Type>(),
                damageType: emitter.DamageType,
                targetVisualEffect: emitter.TargetVisualEffect,
                areaVisualEffect: emitter.AreaVisualEffect,
                playImpactAnimation: false);
        }

        private static void ApplyFieldEngineerPulseEmitterVisual(FieldEngineerPulseEmitter emitter)
        {
            if (emitter.MarkerVisualEffect == VisualEffect.None ||
                !GetIsObjectValid(GetAreaFromLocation(emitter.Location)))
            {
                return;
            }

            EnsureFieldEngineerPulseEmitterMarker(emitter);
            if (GetIsObjectValid(emitter.MarkerObject))
                return;

            var refreshDuration = Math.Min(
                                      Math.Max(emitter.RemainingSeconds, FieldEngineerVisualMinimumDurationSeconds),
                                      FieldEngineerPulseIntervalSeconds) +
                                  FieldEngineerVisualRefreshPaddingSeconds;
            var markerVisualEffectScale = Math.Max(FieldEngineerVisualMinimumDurationSeconds, emitter.MarkerVisualEffectScale);
            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectVisualEffect(emitter.MarkerVisualEffect, false, markerVisualEffectScale),
                emitter.Location,
                refreshDuration);
        }

        private static void EnsureFieldEngineerPulseEmitterMarker(FieldEngineerPulseEmitter emitter)
        {
            if (emitter.MarkerVisualEffect == VisualEffect.None ||
                GetIsObjectValid(emitter.MarkerObject) ||
                !GetIsObjectValid(GetAreaFromLocation(emitter.Location)))
            {
                return;
            }

            var marker = CreateObject(
                ObjectType.Placeable,
                FieldEngineerPulseMarkerResref,
                emitter.Location,
                false,
                FieldEngineerPulseMarkerTag);
            if (!GetIsObjectValid(marker))
                return;

            SetPlotFlag(marker, true);
            emitter.MarkerObject = marker;

            ApplyEffectToObject(
                DurationType.Permanent,
                EffectVisualEffect(
                    emitter.MarkerVisualEffect,
                    false,
                    Math.Max(FieldEngineerVisualMinimumDurationSeconds, emitter.MarkerVisualEffectScale)),
                marker);
        }

        private static bool IsFieldEngineerEmitterTracked(FieldEngineerPulseEmitter emitter)
        {
            return _activeFieldEngineerPulseEmitters.TryGetValue(emitter.Activator, out var emitters) &&
                   emitters.Contains(emitter);
        }

        private static bool IsFieldEngineerEmitterValid(FieldEngineerPulseEmitter emitter)
        {
            return GetIsObjectValid(emitter.Activator) &&
                   GetCurrentHitPoints(emitter.Activator) > 0 &&
                   GetIsObjectValid(GetAreaFromLocation(emitter.Location));
        }

        private static void RemoveFieldEngineerPulseEmitter(FieldEngineerPulseEmitter emitter)
        {
            if (!_activeFieldEngineerPulseEmitters.TryGetValue(emitter.Activator, out var emitters))
                return;

            emitters.Remove(emitter);
            if (GetIsObjectValid(emitter.MarkerObject))
                DestroyObject(emitter.MarkerObject);

            if (emitters.Count <= 0)
                _activeFieldEngineerPulseEmitters.Remove(emitter.Activator);
        }

        private static uint GetNearestHostileCreature(uint activator, Location location, float radius)
        {
            if (!GetIsObjectValid(activator) ||
                GetCurrentHitPoints(activator) <= 0 ||
                !GetIsObjectValid(GetAreaFromLocation(location)))
            {
                return OBJECT_INVALID;
            }

            var nth = 1;
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            while (GetIsObjectValid(creature) &&
                   GetDistanceBetweenLocations(location, GetLocation(creature)) <= radius)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                    return creature;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }

            return OBJECT_INVALID;
        }
    }
}
