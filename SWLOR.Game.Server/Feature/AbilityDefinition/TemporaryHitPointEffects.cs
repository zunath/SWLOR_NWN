using System;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        private const string EffectTagPrefix = "TEMPORARY_HP_";
        private const string BarrierVisualEffectTagSuffix = "_BARRIER_VFX";
        private const string OwnerVariableSuffix = "_OWNER";

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
            DeleteLocalString(target, GetOwnerVariable(effectKey));
            ApplyFlatInternal(target, effectKey, amount, durationSeconds);
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool owned by a companion status-effect instance.
        /// A later application of the same key replaces both the native pool and its owner,
        /// allowing an older status marker to expire without deleting the newer pool.
        /// </summary>
        public static void ApplyFlatOwned(
            uint target,
            string effectKey,
            int amount,
            float durationSeconds,
            string ownerId)
        {
            ValidateEffectKey(effectKey);
            ValidateOwnerId(ownerId);

            ApplyFlatInternal(target, effectKey, amount, durationSeconds);
            if (amount > 0)
                SetLocalString(target, GetOwnerVariable(effectKey), ownerId);
            else
                DeleteLocalString(target, GetOwnerVariable(effectKey));
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
            DeleteLocalString(target, GetOwnerVariable(effectKey));

            var effectTag = EffectTagPrefix + effectKey;
            RemoveEffectByTag(target, effectTag);
            RemoveEffectByTag(target, effectTag + BarrierVisualEffectTagSuffix);
        }

        /// <summary>
        /// Removes a keyed pool only when the caller still owns the most recent application.
        /// </summary>
        public static void RemoveIfCurrent(uint target, string effectKey, string ownerId)
        {
            ValidateEffectKey(effectKey);
            ValidateOwnerId(ownerId);
            if (GetLocalString(target, GetOwnerVariable(effectKey)) != ownerId)
                return;

            Remove(target, effectKey);
        }

        private static string GetOwnerVariable(string effectKey)
        {
            return EffectTagPrefix + effectKey + OwnerVariableSuffix;
        }

        private static void ValidateEffectKey(string effectKey)
        {
            if (string.IsNullOrWhiteSpace(effectKey))
                throw new ArgumentException("Temporary HP effects must declare the ability key they stack under.", nameof(effectKey));
        }

        private static void ValidateOwnerId(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owned temporary HP effects must declare their status-effect owner.", nameof(ownerId));
        }
    }
}
