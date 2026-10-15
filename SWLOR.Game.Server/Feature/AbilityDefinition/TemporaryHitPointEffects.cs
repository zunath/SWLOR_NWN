using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
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
        private const string GrantedVariableSuffix = "_GRANTED";
        // Lets max-HP hooks skip creatures without pools without iterating their effects.
        private const string PoolsActiveUntilVariable = EffectTagPrefix + "POOLS_ACTIVE_UNTIL";
        private const string DeferredTotalVariable = EffectTagPrefix + "DEFERRED_TOTAL";
        private const float PendingRemovalDelaySeconds = 0.1f;

        /// <summary>
        /// The engine reports current HP plus temporary HP as a signed 16-bit value. A larger
        /// total wraps negative and kills the creature, even an immortal one.
        /// </summary>
        public const int MaximumReportedHitPoints = short.MaxValue;

        /// <summary>All temporary HP sources together are capped at this percent of max HP.</summary>
        public const int MaximumTotalPercentOfMaxHP = 50;

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

        public static int GetMaximumTotal(uint target)
        {
            return CalculateMaximumTotal(GetMaxHitPoints(target));
        }

        public static int CalculateMaximumTotal(int maxHitPoints)
        {
            return maxHitPoints <= 0 ? 0 : GameMath.PercentOf(maxHitPoints, MaximumTotalPercentOfMaxHP);
        }

        /// <summary>
        /// How much of a requested pool fits under the cap, given the temporary HP already held from other sources.
        /// </summary>
        public static int CalculateGrantedAmount(int requestedAmount, int maximumTotal, int otherPoolsRemaining)
        {
            return Math.Max(0, Math.Min(requestedAmount, maximumTotal - Math.Max(0, otherPoolsRemaining)));
        }

        /// <summary>
        /// Identifies the latest grant or removal of the keyed pool. It changes whenever the pool
        /// is replaced, removed, or trimmed for any reason other than absorbed damage.
        /// </summary>
        public static int GetGrantGeneration(uint target, string effectKey)
        {
            return GetLocalInt(target, GetGenerationVariable(effectKey));
        }

        /// <summary>
        /// The amount the latest grant of the keyed pool accepted, including any deferred part.
        /// </summary>
        public static int GetGrantedAmount(uint target, string effectKey)
        {
            return GetLocalInt(target, GetGrantedVariable(effectKey));
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
            return Math.Max(GetMaxHitPoints(target), ObjectPlugin.GetCurrentHitPoints(target)) +
                   GetVitalityPenaltyHitPoints(target);
        }

        // When a Vitality penalty ends, the engine restores its hit points through the wrapped
        // 16-bit total and kills a creature at the limit, so room for them is held back up front.
        private static int GetVitalityPenaltyHitPoints(uint target)
        {
            var penalty = 0;
            foreach (var statusEffect in StatusEffect.GetCreatureStatusEffects(target).GetAllEffects())
            {
                if (statusEffect.StatGroup.Abilities.TryGetValue(AbilityType.Vitality, out var vitality) && vitality < 0)
                    penalty -= vitality;
            }

            return penalty <= 0 ? 0 : GetHitDice(target) * ((penalty + 1) / 2);
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

        public static int ApplyFlatPlusPercent(
            uint target,
            string effectKey,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            return ApplyFlat(target, effectKey, amount, durationSeconds);
        }

        // Temporary HP pools from different abilities stack, but an ability never stacks with
        // itself: each grant is tagged with its ability's key and the most recent cast replaces
        // any prior pool carrying the same key, regardless of caster.
        /// <returns>The amount granted, which is smaller than requested under the cap or at the engine's HP limit.</returns>
        public static int ApplyFlat(uint target, string effectKey, int amount, float durationSeconds)
        {
            return ApplyFlatFromSource(OBJECT_INVALID, target, effectKey, amount, durationSeconds);
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool and records the creature that granted it. Source
        /// ownership is only considered active while the matching native temporary-HP effect
        /// still exists, so consuming the pool immediately invalidates source-dependent riders.
        /// </summary>
        /// <returns>The amount granted, which is smaller than requested under the cap or at the engine's HP limit.</returns>
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
            var grantedAmount = ApplyFlatInternal(target, effectKey, amount, durationSeconds, source);

            if (grantedAmount > 0 && GetIsObjectValid(source))
                SetLocalObject(target, GetSourceVariable(effectKey), source);

            return grantedAmount;
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool owned by a companion status-effect instance.
        /// A later application of the same key replaces both the native pool and its owner,
        /// allowing an older status marker to expire without deleting the newer pool.
        /// </summary>
        public static int ApplyFlatOwned(
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

            return grantedAmount;
        }

        /// <summary>
        /// Replaces the keyed pool, clamped under the total cap and so the engine's reported HP
        /// cannot overflow. Room held by pools removed this frame, including the one being
        /// replaced, is reserved and granted in a follow-up effect with the same tag once the
        /// engine releases it.
        /// </summary>
        /// <param name="recordGrant">False when a pool is re-applied smaller rather than granted anew.</param>
        private static int ApplyFlatInternal(
            uint target, string effectKey, int amount, float durationSeconds, uint source = OBJECT_INVALID, bool recordGrant = true)
        {
            var effectTag = EffectTagPrefix + effectKey;
            var generation = AdvanceGeneration(target, effectKey);
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordTemporaryHitPointPoolRemoving(target);
            RemoveEffectByTag(target, effectTag);

            // Immediate room counts pools the engine has not released yet; released room is what
            // remains once it does, less room already reserved for other deferred grants.
            var hitPoints = GetLimitingHitPoints(target);
            var immediateRoom = GetCapacity(hitPoints, GetEngineTemporaryHitPoints(target));
            var releasedRoom = GetCapacity(hitPoints, GetRemaining(target) + GetLocalInt(target, DeferredTotalVariable));
            // Every source shares the total cap. The pool being replaced is already gone, and
            // reserved deferred grants still count against it.
            var capRoom = CalculateGrantedAmount(
                amount, GetMaximumTotal(target), GetRemaining(target) + GetLocalInt(target, DeferredTotalVariable));
            var grantedAmount = Math.Min(capRoom, releasedRoom);
            if (grantedAmount <= 0)
            {
                DeleteLocalInt(target, GetGrantedVariable(effectKey));
                return 0;
            }

            var immediateAmount = Math.Min(grantedAmount, immediateRoom);
            if (immediateAmount > 0)
                ApplyPoolEffect(target, effectTag, immediateAmount, durationSeconds);

            var deferredAmount = grantedAmount - immediateAmount;
            if (deferredAmount > 0 && durationSeconds <= PendingRemovalDelaySeconds)
            {
                grantedAmount = immediateAmount;
                deferredAmount = 0;
            }

            SetLocalInt(target, GetGrantedVariable(effectKey), grantedAmount);
            if (recordGrant && CombatRecorder.IsRecording)
                CombatRecorder.RecordTemporaryHitPointsGranted(source, target, effectKey, grantedAmount, durationSeconds);
            if (deferredAmount <= 0)
                return grantedAmount;

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
            var remainingAmount = amount;
            if (HasEffectWithTag(target, pendingTag))
            {
                RemoveEffectByTag(target, pendingTag);
                // The cap can fall while the grant waits, such as when a Vitality buff wears off, so it is
                // checked again here and the part it no longer allows is dropped instead of waiting.
                var allowedAmount = CalculateGrantedAmount(
                    amount, GetMaximumTotal(target), GetRemaining(target) + GetLocalInt(target, DeferredTotalVariable));
                var appliedAmount = Math.Min(allowedAmount, GetCapacity(target));
                if (appliedAmount > 0)
                    ApplyPoolEffect(target, EffectTagPrefix + effectKey, appliedAmount, durationSeconds);

                remainingAmount = allowedAmount - appliedAmount;
                if (allowedAmount < amount)
                {
                    var kept = GetRemaining(target, effectKey) + remainingAmount;
                    SetLocalInt(target, GetGrantedVariable(effectKey),
                        Math.Max(0, GetLocalInt(target, GetGrantedVariable(effectKey)) - (amount - allowedAmount)));
                    if (CombatRecorder.IsRecording)
                        CombatRecorder.RecordTemporaryHitPointsTrimmed(target, effectKey, amount - allowedAmount, kept);
                    NotifyPoolTrimmed(target, effectKey, kept);
                    if (kept <= 0)
                    {
                        Remove(target, effectKey);
                        return;
                    }
                }

                if (remainingAmount > 0 && GetPendingRemoval(target) > 0 && durationSeconds > PendingRemovalDelaySeconds)
                {
                    ScheduleDeferredGrant(target, effectKey, generation, remainingAmount, durationSeconds - PendingRemovalDelaySeconds);
                    return;
                }
            }

            if (remainingAmount <= 0)
                return;

            // A pool that never landed must not keep its source, owner or barrier visual.
            var landedAmount = GetRemaining(target, effectKey);
            if (landedAmount <= 0)
            {
                Remove(target, effectKey);
                return;
            }

            // Mark the shortfall as a new grant so companions tracking the pool reconcile.
            AdvanceGeneration(target, effectKey);
            SetLocalInt(target, GetGrantedVariable(effectKey), landedAmount);
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

            var activeUntil = GetUnixTimeSeconds() + (int)Math.Ceiling(durationSeconds);
            if (activeUntil > GetLocalInt(target, PoolsActiveUntilVariable))
                SetLocalInt(target, PoolsActiveUntilVariable, activeUntil);
        }

        private static int GetUnixTimeSeconds()
        {
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
        /// Schedules <see cref="EnforceLimit"/> after a change to maximum HP, such as an HP budget
        /// or a native Vitality effect. The check runs once the change has settled and never
        /// iterates effects synchronously, so it is safe inside effect removal callbacks.
        /// </summary>
        public static void ScheduleLimitCheck(uint target)
        {
            if (!GetIsObjectValid(target) || GetUnixTimeSeconds() > GetLocalInt(target, PoolsActiveUntilVariable))
                return;

            AssignCommand(GetModule(), () => DelayCommand(PendingRemovalDelaySeconds, () =>
            {
                if (GetIsObjectValid(target))
                    EnforceLimit(target);
            }));
        }

        /// <summary>
        /// Trims pools back under the total cap after maximum HP drops, and re-applies them through
        /// the clamp after it rises, so healing toward the new maximum cannot push the reported
        /// total past the engine limit.
        /// </summary>
        private static void EnforceLimit(uint target)
        {
            EnforceMaximumTotal(target);

            var hitPoints = GetLimitingHitPoints(target);
            if (hitPoints + GetRemaining(target) <= MaximumReportedHitPoints)
                return;

            var pools = GetKeyedPools(target);

            // Trim the most recently granted pools first.
            for (var index = pools.Count - 1; index >= 0; index--)
            {
                if (hitPoints + GetRemaining(target) <= MaximumReportedHitPoints)
                    return;

                var (effectKey, amount, durationSeconds) = pools[index];
                var kept = ApplyFlatInternal(target, effectKey, amount, durationSeconds, recordGrant: false);
                if (kept < amount && CombatRecorder.IsRecording)
                    CombatRecorder.RecordTemporaryHitPointsTrimmed(target, effectKey, amount - kept, kept);
                if (kept <= 0)
                    Remove(target, effectKey);
            }
        }

        /// <summary>
        /// Trims pools back under the cap after maximum HP drops, such as when food wears off or a Vitality
        /// penalty lands, taking first from the pools that end soonest.
        /// </summary>
        public static void EnforceMaximumTotal(uint target)
        {
            var excess = GetRemaining(target) - GetMaximumTotal(target);
            if (excess <= 0)
                return;

            var pools = GetKeyedPools(target).OrderBy(pool => pool.DurationSeconds).ToList();
            var kept = CalculateTrimmedAmounts(pools.Select(pool => pool.Amount).ToList(), excess);
            for (var index = 0; index < pools.Count; index++)
            {
                var (effectKey, amount, durationSeconds) = pools[index];
                if (kept[index] == amount)
                    continue;

                if (CombatRecorder.IsRecording)
                    CombatRecorder.RecordTemporaryHitPointsTrimmed(target, effectKey, amount - kept[index], kept[index]);
                if (kept[index] <= 0)
                    Remove(target, effectKey);
                else
                    ApplyFlatInternal(target, effectKey, kept[index], Math.Max(1, durationSeconds), recordGrant: false);

                NotifyPoolTrimmed(target, effectKey, kept[index]);
            }
        }

        // Status effects that track a pool, such as Guardian's Resolve, follow it down when it is trimmed.
        private static void NotifyPoolTrimmed(uint target, string effectKey, int kept)
        {
            var trackers = StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<ITemporaryHitPointPoolStatusEffect>()
                .Where(tracker => tracker.TemporaryHitPointPoolKey == effectKey)
                .ToList();
            foreach (var tracker in trackers)
                tracker.OnTemporaryHitPointPoolTrimmed(target, kept);
        }

        /// <summary>What each pool keeps after <paramref name="excess"/> is taken from the pools in order.</summary>
        public static List<int> CalculateTrimmedAmounts(IReadOnlyList<int> amounts, int excess)
        {
            var kept = new List<int>(amounts.Count);
            foreach (var amount in amounts)
            {
                var cut = Math.Clamp(excess, 0, Math.Max(0, amount));
                kept.Add(amount - cut);
                excess -= cut;
            }

            return kept;
        }

        // The keyed pools on the target, one entry per key with its total and longest remaining duration.
        private static List<(string EffectKey, int Amount, int DurationSeconds)> GetKeyedPools(uint target)
        {
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

            return pools;
        }

        /// <summary>
        /// Returns the ability key of a temporary-HP effect tag, or the tag itself when it was
        /// not applied through this class.
        /// </summary>
        public static string GetEffectKey(string effectTag)
        {
            return effectTag != null && effectTag.StartsWith(EffectTagPrefix, StringComparison.Ordinal)
                ? effectTag.Substring(EffectTagPrefix.Length)
                : effectTag;
        }

        public static int ApplyFlatWithBarrierVisual(
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
                return grantedAmount;

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), visualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
            return grantedAmount;
        }

        public static void Remove(uint target, string effectKey)
        {
            ValidateEffectKey(effectKey);
            DeleteLocalString(target, GetOwnerVariable(effectKey));
            DeleteLocalObject(target, GetSourceVariable(effectKey));
            DeleteLocalInt(target, GetGrantedVariable(effectKey));
            AdvanceGeneration(target, effectKey);

            var effectTag = EffectTagPrefix + effectKey;
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordTemporaryHitPointPoolRemoving(target);
            RemoveEffectByTag(target, effectTag);
            RemoveEffectByTag(target, effectTag + BarrierVisualEffectTagSuffix);
        }

        public static bool IsActivePoolFromSource(uint target, string effectKey, uint source)
        {
            ValidateEffectKey(effectKey);
            if (!GetIsObjectValid(source) || GetLocalObject(target, GetSourceVariable(effectKey)) != source)
                return false;

            // A pool still waiting for the engine to release room counts as active, so riders
            // tied to it survive until it lands.
            var effectTag = EffectTagPrefix + effectKey;
            var pendingTag = GetPendingTag(effectKey);
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectTag(effect) == effectTag || GetEffectTag(effect) == pendingTag)
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

        private static string GetGrantedVariable(string effectKey)
        {
            return EffectTagPrefix + effectKey + GrantedVariableSuffix;
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
