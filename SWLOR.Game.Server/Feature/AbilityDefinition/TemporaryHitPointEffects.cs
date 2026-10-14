using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
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

        /// <summary>All temporary HP sources together are capped at this percent of max HP.</summary>
        public const int MaximumTotalPercentOfMaxHP = 50;

        public static int GetRemaining(uint target)
        {
            return GetRemaining(target, null);
        }

        private static int GetRemaining(uint target, string effectTag)
        {
            var total = 0;
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints &&
                    (effectTag == null || GetEffectTag(effect) == effectTag))
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
        // any prior pool carrying the same key, regardless of caster. Returns the amount granted
        // after the total cap.
        public static int ApplyFlat(uint target, string effectKey, int amount, float durationSeconds)
        {
            return ApplyFlatFromSource(OBJECT_INVALID, target, effectKey, amount, durationSeconds);
        }

        /// <summary>
        /// Applies a keyed temporary-HP pool and records the creature that granted it. Source
        /// ownership is only considered active while the matching native temporary-HP effect
        /// still exists, so consuming the pool immediately invalidates source-dependent riders.
        /// </summary>
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
            var granted = ApplyFlatInternal(target, effectKey, amount, durationSeconds, source);

            if (granted > 0 && GetIsObjectValid(source))
                SetLocalObject(target, GetSourceVariable(effectKey), source);

            return granted;
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
            var granted = ApplyFlatInternal(target, effectKey, amount, durationSeconds);
            if (granted > 0)
                SetLocalString(target, GetOwnerVariable(effectKey), ownerId);
            else
                DeleteLocalString(target, GetOwnerVariable(effectKey));

            return granted;
        }

        private static int ApplyFlatInternal(uint target, string effectKey, int amount, float durationSeconds, uint source = OBJECT_INVALID)
        {
            var effectTag = EffectTagPrefix + effectKey;
            // The pool being replaced doesn't count against its own room under the cap.
            var otherPoolsRemaining = GetRemaining(target) - GetRemaining(target, effectTag);
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordTemporaryHitPointPoolRemoving(target);
            RemoveEffectByTag(target, effectTag);

            var granted = CalculateGrantedAmount(amount, GetMaximumTotal(target), otherPoolsRemaining);
            if (granted <= 0)
                return 0;

            ApplyEffectToObject(
                DurationType.Temporary,
                TagEffect(EffectTemporaryHitpoints(granted), effectTag),
                target,
                durationSeconds);

            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordTemporaryHitPointsGranted(source, target, effectKey, granted, durationSeconds);
            return granted;
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
            var granted = ApplyFlat(target, effectKey, amount, durationSeconds);

            // The barrier visual is scoped by the same key as the HP pool: recasting the same
            // ability refreshes its own glow, while other abilities' barrier visuals persist
            // alongside their still-active pools.
            var visualEffectTag = EffectTagPrefix + effectKey + BarrierVisualEffectTagSuffix;
            RemoveEffectByTag(target, visualEffectTag);

            if (granted <= 0 || durationSeconds <= 0f || barrierVisualEffect == VisualEffect.None)
                return granted;

            var visualEffect = TagEffect(EffectVisualEffect(barrierVisualEffect), visualEffectTag);
            ApplyEffectToObject(DurationType.Temporary, visualEffect, target, durationSeconds);
            return granted;
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

            var pools = new List<(Effect Effect, string Tag, int Amount, int SecondsLeft)>();
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                var tag = GetEffectTag(effect);
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints && tag.StartsWith(EffectTagPrefix))
                    pools.Add((effect, tag, Math.Max(0, GetEffectInteger(effect, 0)), GetEffectDurationRemaining(effect)));
            }

            pools = pools.OrderBy(pool => pool.SecondsLeft).ToList();
            var kept = CalculateTrimmedAmounts(pools.Select(pool => pool.Amount).ToList(), excess);
            var trackers = StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<ITemporaryHitPointPoolStatusEffect>()
                .ToList();
            for (var index = 0; index < pools.Count; index++)
            {
                var pool = pools[index];
                if (kept[index] == pool.Amount)
                    continue;

                var effectKey = pool.Tag.Substring(EffectTagPrefix.Length);
                if (CombatRecorder.IsRecording)
                    CombatRecorder.RecordTemporaryHitPointsTrimmed(target, effectKey, pool.Amount - kept[index], kept[index]);
                if (kept[index] <= 0)
                {
                    Remove(target, effectKey);
                }
                else
                {
                    if (CombatRecorder.IsRecording)
                        CombatRecorder.RecordTemporaryHitPointPoolRemoving(target);
                    RemoveEffect(target, pool.Effect);
                    ApplyEffectToObject(
                        DurationType.Temporary,
                        TagEffect(EffectTemporaryHitpoints(kept[index]), pool.Tag),
                        target,
                        Math.Max(1, pool.SecondsLeft));
                }

                foreach (var tracker in trackers.Where(tracker => tracker.TemporaryHitPointPoolKey == effectKey))
                    tracker.OnTemporaryHitPointPoolTrimmed(target, kept[index]);
            }
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

        public static void Remove(uint target, string effectKey)
        {
            ValidateEffectKey(effectKey);
            DeleteLocalString(target, GetOwnerVariable(effectKey));
            DeleteLocalObject(target, GetSourceVariable(effectKey));

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
