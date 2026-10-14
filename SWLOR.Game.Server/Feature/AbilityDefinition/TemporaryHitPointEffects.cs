using System;
using System.Collections.Generic;
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
        private const string DeferredVariableSuffix = "_DEFERRED";
        private const string PendingTagSuffix = "_PENDING";
        private const string DeferredTotalVariable = EffectTagPrefix + "DEFERRED_TOTAL";
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
            return GetCapacity(GetLimitingHitPoints(target), GetEngineTemporaryHitPoints(target));
        }

        private static int GetLimitingHitPoints(uint target)
        {
            return Math.Max(GetMaxHitPoints(target), ObjectPlugin.GetCurrentHitPoints(target));
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
        /// <returns>The amount granted, which is smaller than requested at the engine's HP limit.</returns>
        public static int ApplyFlat(uint target, string effectKey, int amount, float durationSeconds)
        {
            return ApplyFlatFromSource(OBJECT_INVALID, target, effectKey, amount, durationSeconds);
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool and records the creature that granted it. Source
        /// ownership is only considered active while the matching native temporary-HP effect
        /// still exists, so consuming the pool immediately invalidates source-dependent riders.
        /// </summary>
        /// <returns>The amount granted, which is smaller than requested at the engine's HP limit.</returns>
        public static int ApplyFlatFromSource(
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
        /// held by pools removed this frame, including the one being replaced, is reserved and
        /// granted in a follow-up effect with the same tag once the engine releases it.
        /// </summary>
        private static int ApplyFlatInternal(uint target, string effectKey, int amount, float durationSeconds)
        {
            var effectTag = EffectTagPrefix + effectKey;
            var generation = AdvanceGeneration(target, effectKey);
            RemoveEffectByTag(target, effectTag);

            // Immediate room counts pools the engine has not released yet; released room is what
            // remains once it does, less room already reserved for other deferred grants.
            var hitPoints = GetLimitingHitPoints(target);
            var immediateRoom = GetCapacity(hitPoints, GetEngineTemporaryHitPoints(target));
            var releasedRoom = GetCapacity(hitPoints, GetRemaining(target) + GetLocalInt(target, DeferredTotalVariable));
            var grantedAmount = Math.Min(amount, releasedRoom);
            if (grantedAmount <= 0)
                return 0;

            var immediateAmount = Math.Min(grantedAmount, immediateRoom);
            if (immediateAmount > 0)
                ApplyPoolEffect(target, effectTag, immediateAmount, durationSeconds);

            var deferredAmount = grantedAmount - immediateAmount;
            if (deferredAmount <= 0)
                return grantedAmount;

            if (durationSeconds <= PendingRemovalDelaySeconds)
                return immediateAmount;

            ScheduleDeferredGrant(target, effectKey, generation, deferredAmount, durationSeconds - PendingRemovalDelaySeconds);
            return grantedAmount;
        }

        // Reserved room is excluded from every later grant until the deferred effect lands, so
        // grants made while it waits cannot claim the same released pool. The pending marker is
        // an inert effect, so anything that strips the target's effects also cancels the grant.
        private static void ScheduleDeferredGrant(uint target, string effectKey, int generation, int amount, float durationSeconds)
        {
            SetLocalInt(target, GetDeferredVariable(effectKey), amount);
            SetLocalInt(target, DeferredTotalVariable, GetLocalInt(target, DeferredTotalVariable) + amount);
            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectRunScript(), GetPendingTag(effectKey)),
                target,
                durationSeconds + PendingRemovalDelaySeconds);
            AssignCommand(GetModule(), () => DelayCommand(PendingRemovalDelaySeconds, () =>
                ApplyDeferredGrant(target, effectKey, generation, amount, durationSeconds)));
        }

        private static void ApplyDeferredGrant(uint target, string effectKey, int generation, int amount, float durationSeconds)
        {
            // A later application or removal of this key already released the reservation.
            if (!GetIsObjectValid(target) || GetLocalInt(target, GetGenerationVariable(effectKey)) != generation)
                return;

            ReleaseDeferredGrant(target, effectKey);
            var pendingTag = GetPendingTag(effectKey);
            if (!HasEffectWithTag(target, pendingTag))
            {
                if (GetRemaining(target, effectKey) <= 0)
                    Remove(target, effectKey);
                return;
            }

            RemoveEffectByTag(target, pendingTag);
            var appliedAmount = Math.Min(amount, GetCapacity(target));
            if (appliedAmount > 0)
                ApplyPoolEffect(target, EffectTagPrefix + effectKey, appliedAmount, durationSeconds);

            var remainingAmount = amount - appliedAmount;
            if (remainingAmount > 0 && GetPendingRemoval(target) > 0 && durationSeconds > PendingRemovalDelaySeconds)
            {
                ScheduleDeferredGrant(target, effectKey, generation, remainingAmount, durationSeconds - PendingRemovalDelaySeconds);
                return;
            }

            // A pool that never landed must not keep its source, owner or barrier visual.
            if (GetRemaining(target, effectKey) <= 0)
                Remove(target, effectKey);
        }

        private static void ReleaseDeferredGrant(uint target, string effectKey)
        {
            var amount = GetLocalInt(target, GetDeferredVariable(effectKey));
            if (amount <= 0)
                return;

            DeleteLocalInt(target, GetDeferredVariable(effectKey));
            var total = GetLocalInt(target, DeferredTotalVariable) - amount;
            if (total > 0)
                SetLocalInt(target, DeferredTotalVariable, total);
            else
                DeleteLocalInt(target, DeferredTotalVariable);
        }

        private static int GetPendingRemoval(uint target)
        {
            return GetEngineTemporaryHitPoints(target) - GetRemaining(target);
        }

        private static void ApplyPoolEffect(uint target, string effectTag, int amount, float durationSeconds)
        {
            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectTemporaryHitpoints(amount), effectTag),
                target,
                durationSeconds);
        }

        // Cancels and releases any deferred grant still waiting for the previous application of this key.
        private static int AdvanceGeneration(uint target, string effectKey)
        {
            ReleaseDeferredGrant(target, effectKey);
            RemoveEffectByTag(target, GetPendingTag(effectKey));
            var generation = GetLocalInt(target, GetGenerationVariable(effectKey)) + 1;
            SetLocalInt(target, GetGenerationVariable(effectKey), generation);
            return generation;
        }

        /// <summary>
        /// Re-applies pools through the clamp after the target's maximum HP rises, so healing
        /// toward the new maximum cannot push the reported total past the engine limit.
        /// </summary>
        public static void EnforceLimit(uint target)
        {
            var hitPoints = GetLimitingHitPoints(target);
            if (hitPoints + GetRemaining(target) <= MaximumReportedHitPoints)
                return;

            var pools = new List<(string EffectKey, int Amount, int DurationSeconds)>();
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                var effectTag = GetEffectTag(effect);
                if (GetEffectType(effect) != EffectTypeScript.TemporaryHitpoints ||
                    GetEffectDurationType(effect) != (int)DurationType.Temporary ||
                    !effectTag.StartsWith(EffectTagPrefix, StringComparison.Ordinal))
                    continue;

                var effectKey = effectTag.Substring(EffectTagPrefix.Length);
                var index = pools.FindIndex(pool => pool.EffectKey == effectKey);
                var amount = Math.Max(0, GetEffectInteger(effect, 0));
                var duration = GetEffectDurationRemaining(effect);
                if (index < 0)
                    pools.Add((effectKey, amount, duration));
                else
                    pools[index] = (effectKey, pools[index].Amount + amount, Math.Max(pools[index].DurationSeconds, duration));
            }

            // Trim the most recently granted pools first.
            for (var index = pools.Count - 1; index >= 0; index--)
            {
                if (hitPoints + GetRemaining(target) <= MaximumReportedHitPoints)
                    return;

                var (effectKey, amount, durationSeconds) = pools[index];
                if (ApplyFlatInternal(target, effectKey, amount, durationSeconds) <= 0)
                    Remove(target, effectKey);
            }
        }

        public static void ApplyFlatWithBarrierVisual(
            uint target,
            string effectKey,
            int amount,
            float durationSeconds,
            VisualEffect barrierVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue)
        {
            var grantedAmount = ApplyFlat(target, effectKey, amount, durationSeconds);

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

        private static string GetDeferredVariable(string effectKey)
        {
            return EffectTagPrefix + effectKey + DeferredVariableSuffix;
        }

        private static string GetPendingTag(string effectKey)
        {
            return EffectTagPrefix + effectKey + PendingTagSuffix;
        }

        private static bool HasEffectWithTag(uint target, string effectTag)
        {
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectTag(effect) == effectTag)
                    return true;
            }

            return false;
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
