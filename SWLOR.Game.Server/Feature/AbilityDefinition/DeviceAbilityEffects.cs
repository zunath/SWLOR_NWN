using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
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
                bool isAreaPulse,
                bool appliesBeaconPulseBonuses,
                bool showAreaIndicator)
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
                AppliesBeaconPulseBonuses = appliesBeaconPulseBonuses;
                ShowAreaIndicator = showAreaIndicator;
                MarkerObject = OBJECT_INVALID;
                AreaIndicatorId = string.Empty;
                ApplyPulse = Ability.CaptureRepeatedAbilityImpact(activator, () =>
                {
                    if (IsAreaPulse)
                        ApplyAreaHostilePulse(this);
                    else
                        ApplySingleHostilePulse(this);
                });
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
            public bool AppliesBeaconPulseBonuses { get; }
            public bool ShowAreaIndicator { get; }
            public uint MarkerObject { get; set; }
            public string AreaIndicatorId { get; set; }
            public Action ApplyPulse { get; }
        }

        public static void ApplyPowerSurge(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.PowerCellInitialTargetPowerSurge) <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, new PowerSurgeStatusEffect(), 30f);
        }

        public static void ApplyFieldSupportAllyBuffRiders(uint activator, uint target)
        {
            ApplyRayshieldScreenRider(activator, target);
            ApplyDampeningFieldRider(activator, target);
            ApplyOverclockRoutine(activator, target);
        }

        public static void ApplyTacticalUplink(uint activator)
        {
            if (Stat.GetStatAdjustment(activator, StatType.AssaultGadgetTacticalUplink) <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, new TacticalUplinkStatusEffect(), 30f);
            }
        }

        public static void ApplyElectricArcVisual(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            var electricArc = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand);
            var electricBurst = EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, electricArc, target, 1.5f);
                ApplyEffectToObject(DurationType.Instant, electricBurst, target);
            });
        }

        public static void ApplyDiagnosticSweep(uint activator, Location location, float radius)
        {
            var revealsHidden = Stat.GetStatAdjustment(activator, StatType.FieldEngineerAreaRevealHidden) > 0;
            var evasionPenalty = Stat.GetStatAdjustment(activator, StatType.FieldEngineerAreaEvasionPenaltyPercent);
            var durationSeconds = Stat.GetStatAdjustment(activator, StatType.FieldEngineerAreaEvasionPenaltyDurationSeconds);
            if (!revealsHidden && (evasionPenalty <= 0 || durationSeconds <= 0))
                return;

            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            while (GetIsObjectValid(creature))
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    if (revealsHidden)
                    {
                        SetActionMode(creature, ActionMode.Stealth, false);
                        RemoveEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
                    }

                    if (evasionPenalty > 0 && durationSeconds > 0)
                    {
                        StatusEffect.ApplyStatusEffect(
                            activator,
                            creature,
                            new DiagnosticSweepStatusEffect(evasionPenalty),
                            durationSeconds);
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }
        }

        private static void ApplyRayshieldScreenRider(uint activator, uint target)
        {
            var physicalDefensePercent = Stat.GetStatAdjustment(
                activator,
                StatType.FieldSupportPhysicalDefensePercent);
            var durationSeconds = Stat.GetStatAdjustment(
                activator,
                StatType.FieldSupportPhysicalDefenseDurationSeconds);
            if (physicalDefensePercent <= 0 || durationSeconds <= 0)
                return;

            StatusEffectBase statusEffect = physicalDefensePercent >= 12
                ? new RayshieldScreen2StatusEffect()
                : new RayshieldScreen1StatusEffect();

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, durationSeconds);
        }

        private static void ApplyDampeningFieldRider(uint activator, uint target)
        {
            var reductionPercent = Stat.GetStatAdjustment(
                activator,
                StatType.FieldSupportPhysicalAndForceDamageReductionPercent);
            var durationSeconds = Stat.GetStatAdjustment(
                activator,
                StatType.FieldSupportPhysicalAndForceDamageReductionDurationSeconds);
            if (reductionPercent <= 0 || durationSeconds <= 0)
                return;

            StatusEffectBase statusEffect = reductionPercent >= 10
                ? new DampeningField2StatusEffect()
                : new DampeningField1StatusEffect();

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, durationSeconds);
        }

        private static void ApplyOverclockRoutine(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.FieldSupportAllyOverclockRoutine) <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, new OverclockRoutineStatusEffect(), 30f);
        }

        public static float ApplyBlastRadiusBonus(uint activator, float baseRadius)
        {
            return CalculateBlastRadius(
                baseRadius,
                Stat.GetStatAdjustment(activator, StatType.BlastRadiusBonusTenths));
        }

        public static float ApplyBeaconPulseRangeBonus(uint activator, float baseRadius)
        {
            return baseRadius + Stat.GetStatAdjustment(activator, StatType.BeaconPulseRangeBonusMeters);
        }

        public static float CalculateBlastRadius(float baseRadius, int radiusBonusTenths)
        {
            return baseRadius + radiusBonusTenths / 10f;
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

        public static Func<uint, int> GetAssaultGadgetBaseDamageAdjustment(uint activator)
        {
            var adjustment = CalculateAssaultGadgetWeaponDamageEquivalent(
                Skill.GetCreatureSkillRank(activator, SkillType.Devices));

            return adjustment == 0
                ? null
                : _ => adjustment;
        }

        public static int CalculateAssaultGadgetWeaponDamageEquivalent(int devicesRank)
        {
            if (devicesRank >= 50)
                return 28;

            if (devicesRank >= 40)
                return 24;

            if (devicesRank >= 30)
                return 19;

            if (devicesRank >= 20)
                return 15;

            if (devicesRank >= 10)
                return 10;

            return 6;
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
            float markerVisualEffectScale = 1f,
            bool showAreaIndicator = true)
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
                false,
                true,
                showAreaIndicator));
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
            float markerVisualEffectScale = 1f,
            bool appliesBeaconPulseBonuses = false,
            bool showAreaIndicator = true)
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
                true,
                appliesBeaconPulseBonuses,
                showAreaIndicator));
        }

        public static uint CreateTemporaryFieldEngineerMarker(
            Location location,
            VisualEffect markerVisualEffect,
            float markerVisualEffectScale,
            float durationSeconds,
            string markerResref = FieldEngineerPulseMarkerResref)
        {
            if (markerVisualEffect == VisualEffect.None ||
                durationSeconds <= 0f ||
                !GetIsObjectValid(GetAreaFromLocation(location)))
            {
                return OBJECT_INVALID;
            }

            var marker = CreateObject(
                ObjectType.Placeable,
                markerResref,
                location,
                false,
                FieldEngineerPulseMarkerTag);
            if (!GetIsObjectValid(marker))
            {
                Log.Write(LogGroup.Error,
                    $"Failed to create field marker placeable '{markerResref}' at the requested location.");
                return OBJECT_INVALID;
            }

            SetPlotFlag(marker, true);
            ApplyEffectToObject(
                DurationType.Permanent,
                EffectVisualEffect(
                    markerVisualEffect,
                    false,
                    Math.Max(FieldEngineerVisualMinimumDurationSeconds, markerVisualEffectScale)),
                marker);
            DestroyObject(marker, durationSeconds);

            return marker;
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
                RefreshFieldEngineerPulseEmitterIndicator(emitter);
                extendedAny = true;
            }

            return extendedAny;
        }

        public static bool TriggerActiveFieldEngineerPulses(uint activator)
        {
            if (!_activeFieldEngineerPulseEmitters.TryGetValue(activator, out var emitters))
                return false;

            var triggeredAny = false;
            foreach (var emitter in emitters.ToArray())
            {
                if (!IsFieldEngineerEmitterValid(emitter))
                {
                    RemoveFieldEngineerPulseEmitter(emitter);
                    continue;
                }

                ApplyFieldEngineerPulseEmitterVisual(emitter);

                emitter.ApplyPulse();

                triggeredAny = true;
            }

            return triggeredAny;
        }

        private static void TrackFieldEngineerPulseEmitter(FieldEngineerPulseEmitter emitter)
        {
            if (!_activeFieldEngineerPulseEmitters.TryGetValue(emitter.Activator, out var emitters))
            {
                emitters = new List<FieldEngineerPulseEmitter>();
                _activeFieldEngineerPulseEmitters[emitter.Activator] = emitters;
            }

            emitters.Add(emitter);
            RefreshFieldEngineerPulseEmitterIndicator(emitter);

            EnsureFieldEngineerPulseEmitterMarker(emitter);
            ApplyFieldEngineerPulseEmitterVisual(emitter);
            ScheduleNextFieldEngineerPulse(emitter);
        }

        private static void RefreshFieldEngineerPulseEmitterIndicator(FieldEngineerPulseEmitter emitter)
        {
            if (!emitter.ShowAreaIndicator)
                return;

            if (!string.IsNullOrWhiteSpace(emitter.AreaIndicatorId))
                Telegraph.CancelTelegraph(emitter.AreaIndicatorId);

            var indicatorRadius = emitter.AppliesBeaconPulseBonuses
                ? ApplyBeaconPulseRangeBonus(emitter.Activator, emitter.Radius)
                : emitter.Radius;
            emitter.AreaIndicatorId = AbilityAreaEffects.CreatePersistentSphereIndicator(
                emitter.Activator,
                emitter.Location,
                indicatorRadius,
                emitter.RemainingSeconds,
                true);
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

                emitter.ApplyPulse();

                if (emitter.RemainingSeconds > 0.01f)
                    ScheduleNextFieldEngineerPulse(emitter);
                else
                    RemoveFieldEngineerPulseEmitter(emitter);
            });
        }

        private static void ApplySingleHostilePulse(FieldEngineerPulseEmitter emitter)
        {
            var radius = ApplyBeaconPulseRangeBonus(emitter.Activator, emitter.Radius);
            ApplyDiagnosticSweep(emitter.Activator, emitter.Location, radius);

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
                playImpactAnimation: false,
                combatImpactDamageAbility: AbilityType.Perception,
                resolvesHit: false,
                canCritical: false);
        }

        private static void ApplyAreaHostilePulse(FieldEngineerPulseEmitter emitter)
        {
            var radius = emitter.Radius;
            Func<uint, int> damagePercentAdjustment = null;
            var resolvesHit = true;
            var canCritical = true;

            if (emitter.AppliesBeaconPulseBonuses)
            {
                radius = ApplyBeaconPulseRangeBonus(emitter.Activator, radius);
                var damageBonus = Stat.GetStatAdjustment(emitter.Activator, StatType.BeaconPulseDamagePercentAdjustment);
                damagePercentAdjustment = damageBonus == 0
                    ? null
                    : new Func<uint, int>(_ => damageBonus);
                resolvesHit = false;
                canCritical = false;
            }

            ApplyDiagnosticSweep(emitter.Activator, emitter.Location, radius);

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
                radius,
                0f,
                Array.Empty<Type>(),
                damageType: emitter.DamageType,
                targetVisualEffect: emitter.TargetVisualEffect,
                areaVisualEffect: emitter.AreaVisualEffect,
                damagePercentAdjustment: damagePercentAdjustment,
                playImpactAnimation: false,
                combatImpactDamageAbility: AbilityType.Perception,
                resolvesHit: resolvesHit,
                canCritical: canCritical);
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
            if (!string.IsNullOrWhiteSpace(emitter.AreaIndicatorId))
                Telegraph.CancelTelegraph(emitter.AreaIndicatorId);

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

            var nearestCreature = OBJECT_INVALID;
            var nearestDistance = float.MaxValue;
            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true, ObjectType.Creature);

            while (GetIsObjectValid(creature))
            {
                if (!GetIsDead(creature) &&
                    GetCurrentHitPoints(creature) > 0 &&
                    GetIsReactionTypeHostile(creature, activator))
                {
                    var distance = GetDistanceBetweenLocations(location, GetLocation(creature));
                    if (distance < nearestDistance)
                    {
                        nearestCreature = creature;
                        nearestDistance = distance;
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true, ObjectType.Creature);
            }

            return nearestCreature;
        }
    }
}
