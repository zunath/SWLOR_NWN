using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        private const string EffectTagPrefix = "TEMPORARY_HP_";
        private const string BarrierVisualEffectTagSuffix = "_BARRIER_VFX";
        private static readonly Dictionary<(uint Target, string EffectKey), long> _trackedApplications = new();
        private static long _nextTrackedApplicationId;

        public static void ApplyFlatPlusPercent(
            uint target,
            string effectKey,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            ApplyFlat(target, effectKey, amount, durationSeconds);
        }

        // Temporary HP pools from different abilities stack, but an ability never stacks with
        // itself: each grant is tagged with its ability's key and the most recent cast replaces
        // any prior pool carrying the same key, regardless of caster.
        public static void ApplyFlat(uint target, string effectKey, int amount, float durationSeconds)
        {
            ValidateEffectKey(effectKey);
            _trackedApplications.Remove((target, effectKey));
            ApplyFlatInternal(target, effectKey, amount, durationSeconds);
        }

        // Returns an ownership token for a companion status marker. When another rank or caster
        // replaces the same keyed pool, an older marker can expire without removing the new pool.
        public static long ApplyFlatTracked(uint target, string effectKey, int amount, float durationSeconds)
        {
            ValidateEffectKey(effectKey);

            var applicationId = ++_nextTrackedApplicationId;
            _trackedApplications[(target, effectKey)] = applicationId;
            ApplyFlatInternal(target, effectKey, amount, durationSeconds);
            return applicationId;
        }

        private static void ApplyFlatInternal(uint target, string effectKey, int amount, float durationSeconds)
        {
            var effectTag = EffectTagPrefix + effectKey;
            RemoveEffectByTag(target, effectTag);

            if (amount <= 0)
                return;

            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectTemporaryHitpoints(amount), effectTag),
                target,
                durationSeconds);
        }

        public static void ApplyFlatWithBarrierVisual(
            uint target,
            string effectKey,
            int amount,
            float durationSeconds,
            VisualEffect barrierVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue)
        {
            ApplyFlat(target, effectKey, amount, durationSeconds);

            // The barrier visual is scoped by the same key as the HP pool: recasting the same
            // ability refreshes its own glow, while other abilities' barrier visuals persist
            // alongside their still-active pools.
            var visualEffectTag = EffectTagPrefix + effectKey + BarrierVisualEffectTagSuffix;
            RemoveEffectByTag(target, visualEffectTag);

            if (amount <= 0 || durationSeconds <= 0f || barrierVisualEffect == VisualEffect.None)
                return;

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), visualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
        }

        public static void Remove(uint target, string effectKey)
        {
            ValidateEffectKey(effectKey);
            _trackedApplications.Remove((target, effectKey));

            var effectTag = EffectTagPrefix + effectKey;
            RemoveEffectByTag(target, effectTag);
            RemoveEffectByTag(target, effectTag + BarrierVisualEffectTagSuffix);
        }

        public static void RemoveIfCurrent(uint target, string effectKey, long applicationId)
        {
            ValidateEffectKey(effectKey);
            if (!_trackedApplications.TryGetValue((target, effectKey), out var currentApplicationId) ||
                currentApplicationId != applicationId)
            {
                return;
            }

            Remove(target, effectKey);
        }

        private static void ValidateEffectKey(string effectKey)
        {
            if (string.IsNullOrWhiteSpace(effectKey))
                throw new ArgumentException("Temporary HP effects must declare the ability key they stack under.", nameof(effectKey));
        }
    }
}
