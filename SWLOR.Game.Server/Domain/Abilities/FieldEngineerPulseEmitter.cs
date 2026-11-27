using System;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// One active Field Engineer pulse emitter: what it pulses, where and for how long. The pulse delegates are bound by the
    /// ability definition that creates it (<c>DeviceAbilityEffects</c>), so this state lives below the content that runs it.
    /// </summary>
    public sealed class FieldEngineerPulseEmitter
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
            bool showAreaIndicator,
            VisualEffect projectileVisualEffect = VisualEffect.None,
            VisualEffect beamVisualEffect = VisualEffect.None)
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
            ProjectileVisualEffect = projectileVisualEffect;
            BeamVisualEffect = beamVisualEffect;
            MarkerObject = OBJECT_INVALID;
            AreaIndicatorId = string.Empty;
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
        public VisualEffect ProjectileVisualEffect { get; }
        public VisualEffect BeamVisualEffect { get; }
        public bool HasShotVisual => ProjectileVisualEffect != VisualEffect.None || BeamVisualEffect != VisualEffect.None;
        public uint MarkerObject { get; set; }
        public string AreaIndicatorId { get; set; }
        public Action ApplyPulse { get; internal set; }
        public Action<uint> ApplyTargetImpact { get; internal set; }
        public Func<int, Action<uint>> CreateAreaImpactBatch { get; internal set; }
        public int PendingProjectileImpacts { get; set; }
    }
}
