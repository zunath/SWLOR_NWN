using System;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class TemporaryHitPointEffects
    {
        private const string EffectTagPrefix = "TEMPORARY_HP_";
        private const string BarrierVisualEffectTagSuffix = "_BARRIER_VFX";
        private const string OwnerVariableSuffix = "_OWNER";
        private const string SourceVariableSuffix = "_SOURCE";
        private const string GenerationVariableSuffix = "_GENERATION";
        private const float PendingRemovalDelaySeconds = 0.1f;

        /// <summary>
        /// The engine reports current HP plus temporary HP as a signed 16-bit value. A larger
        /// total wraps negative and kills the creature, even an immortal one.
        /// </summary>
        public const int MaximumReportedHitPoints = short.MaxValue;

        public static int GetRemaining(uint target)
        {
            var total = 0;
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    total += Math.Max(0, GetEffectInteger(effect, 0));
            }

            return total;
        }

        public static int GetRemaining(uint target, string effectKey)
        {
            ValidateEffectKey(effectKey);
            var effectTag = EffectTagPrefix + effectKey;
            var total = 0;
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints && GetEffectTag(effect) == effectTag)
                    total += Math.Max(0, GetEffectInteger(effect, 0));
            }

            return total;
        }

        /// <summary>
        /// Returns how much more temporary HP the target can hold before the engine's reported
        /// HP overflows. Measured from maximum HP, not current HP, so healing while a pool is
        /// active cannot push the total past the limit.
        /// </summary>
        public static int GetCapacity(uint target)
        {
            var hitPoints = Math.Max(GetMaxHitPoints(target), ObjectPlugin.GetCurrentHitPoints(target));
            return GetCapacity(hitPoints, GetEngineTemporaryHitPoints(target));
        }

        public static int GetCapacity(int hitPoints, int remainingTemporaryHitPoints)
        {
            return Math.Max(0, MaximumReportedHitPoints - hitPoints - remainingTemporaryHitPoints);
        }

        /// <summary>
        /// Recovers the engine's temporary HP total from its wrapped 16-bit report.
        /// </summary>
        public static int GetEngineTemporaryHitPoints(int reportedHitPoints, int currentHitPoints)
        {
            var difference = (reportedHitPoints - currentHitPoints) % 65536;
            return difference < 0 ? difference + 65536 : difference;
        }

        // A removed pool leaves the effect list at once but stays in the engine's total until
        // the frame ends, so the engine total can exceed the effect sum for one frame.
        private static int GetEngineTemporaryHitPoints(uint target)
        {
            var engineTotal = GetEngineTemporaryHitPoints(GetCurrentHitPoints(target), ObjectPlugin.GetCurrentHitPoints(target));
            return Math.Max(engineTotal, GetRemaining(target));
        }

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
            ApplyFlatFromSource(OBJECT_INVALID, target, effectKey, amount, durationSeconds);
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool and records the creature that granted it. Source
        /// ownership is only considered active while the matching native temporary-HP effect
        /// still exists, so consuming the pool immediately invalidates source-dependent riders.
        /// </summary>
        public static void ApplyFlatFromSource(
            uint source,
            uint target,
            string effectKey,
            int amount,
            float durationSeconds)
        {
            ApplySourcedPool(source, target, effectKey, amount, durationSeconds);
        }

        private static int ApplySourcedPool(
            uint source,
            uint target,
            string effectKey,
            int amount,
            float durationSeconds)
        {
            ValidateEffectKey(effectKey);
            DeleteLocalString(target, GetOwnerVariable(effectKey));
            DeleteLocalObject(target, GetSourceVariable(effectKey));
            var grantedAmount = ApplyFlatInternal(target, effectKey, amount, durationSeconds);

            if (grantedAmount > 0 && GetIsObjectValid(source))
                SetLocalObject(target, GetSourceVariable(effectKey), source);

            return grantedAmount;
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

            DeleteLocalObject(target, GetSourceVariable(effectKey));
            var grantedAmount = ApplyFlatInternal(target, effectKey, amount, durationSeconds);
            if (grantedAmount > 0)
                SetLocalString(target, GetOwnerVariable(effectKey), ownerId);
            else
                DeleteLocalString(target, GetOwnerVariable(effectKey));
        }

        /// <summary>
        /// Replaces the keyed pool, clamped so the engine's reported HP cannot overflow. Room
        /// held by pools removed this frame, including the one being replaced, is granted in a
        /// follow-up effect with the same tag once the engine releases it.
        /// </summary>
        private static int ApplyFlatInternal(uint target, string effectKey, int amount, float durationSeconds)
        {
            var effectTag = EffectTagPrefix + effectKey;
            var generation = AdvanceGeneration(target, effectKey);
            RemoveEffectByTag(target, effectTag);

            var capacity = GetCapacity(target);
            var pendingRemoval = GetEngineTemporaryHitPoints(target) - GetRemaining(target);
            var grantedAmount = Math.Min(amount, capacity + pendingRemoval);
            if (grantedAmount <= 0)
                return 0;

            var immediateAmount = Math.Min(grantedAmount, capacity);
            if (immediateAmount > 0)
                ApplyPoolEffect(target, effectTag, immediateAmount, durationSeconds);

            var deferredAmount = grantedAmount - immediateAmount;
            if (deferredAmount <= 0)
                return grantedAmount;

            if (durationSeconds <= PendingRemovalDelaySeconds)
                return immediateAmount;

            AssignCommand(GetModule(), () => DelayCommand(PendingRemovalDelaySeconds, () =>
            {
                if (!GetIsObjectValid(target) || GetLocalInt(target, GetGenerationVariable(effectKey)) != generation)
                    return;

                var topUpAmount = Math.Min(deferredAmount, GetCapacity(target));
                if (topUpAmount > 0)
                    ApplyPoolEffect(target, effectTag, topUpAmount, durationSeconds - PendingRemovalDelaySeconds);
            }));
            return grantedAmount;
        }

        private static void ApplyPoolEffect(uint target, string effectTag, int amount, float durationSeconds)
        {
            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectTemporaryHitpoints(amount), effectTag),
                target,
                durationSeconds);
        }

        // Invalidates any deferred grant still waiting for the previous application of this key.
        private static int AdvanceGeneration(uint target, string effectKey)
        {
            var generation = GetLocalInt(target, GetGenerationVariable(effectKey)) + 1;
            SetLocalInt(target, GetGenerationVariable(effectKey), generation);
            return generation;
        }

        public static void ApplyFlatWithBarrierVisual(
            uint target,
            string effectKey,
            int amount,
            float durationSeconds,
            VisualEffect barrierVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue)
        {
            var grantedAmount = ApplySourcedPool(OBJECT_INVALID, target, effectKey, amount, durationSeconds);

            // The barrier visual is scoped by the same key as the HP pool: recasting the same
            // ability refreshes its own glow, while other abilities' barrier visuals persist
            // alongside their still-active pools.
            var visualEffectTag = EffectTagPrefix + effectKey + BarrierVisualEffectTagSuffix;
            RemoveEffectByTag(target, visualEffectTag);

            if (grantedAmount <= 0 || durationSeconds <= 0f || barrierVisualEffect == VisualEffect.None)
                return;

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), visualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
        }

        public static void Remove(uint target, string effectKey)
        {
            ValidateEffectKey(effectKey);
            DeleteLocalString(target, GetOwnerVariable(effectKey));
            DeleteLocalObject(target, GetSourceVariable(effectKey));
            AdvanceGeneration(target, effectKey);

            var effectTag = EffectTagPrefix + effectKey;
            RemoveEffectByTag(target, effectTag);
            RemoveEffectByTag(target, effectTag + BarrierVisualEffectTagSuffix);
        }

        public static bool IsActivePoolFromSource(uint target, string effectKey, uint source)
        {
            ValidateEffectKey(effectKey);
            if (!GetIsObjectValid(source) || GetLocalObject(target, GetSourceVariable(effectKey)) != source)
                return false;

            var effectTag = EffectTagPrefix + effectKey;
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectTag(effect) == effectTag)
                    return true;
            }

            return false;
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

        private static string GetSourceVariable(string effectKey)
        {
            return EffectTagPrefix + effectKey + SourceVariableSuffix;
        }

        private static string GetGenerationVariable(string effectKey)
        {
            return EffectTagPrefix + effectKey + GenerationVariableSuffix;
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
