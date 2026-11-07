using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static readonly Dictionary<(uint, uint), SuppressionAbilityUseState> _pendingSuppressionAbilityUses = new();
        private static readonly HashSet<uint> _containmentNormalizationTargets = new();

        private sealed class SuppressionAbilityUseState
        {
            public DateTime Expiration { get; init; }
            public HashSet<string> SuppressionEffectIds { get; init; } = new();
        }

        private static void ApplyAutoAttackSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponSkillType(skillType))
                return;

            var chance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var applied = ApplySuppressionStack(
                attacker,
                defender,
                Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);

            if (applied && GetIsPC(attacker))
            {
                SendMessageToPC(attacker, ColorToken.Combat("Pinning Fire: Suppression applied."));
                PlayerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat("Pinning Fire"), attacker, false);
            }
        }

        private static void ApplyRangedHitSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponSkillType(skillType))
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackDurationSeconds);
            var penalty = Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackEvasionPenaltyPercent);
            if (duration > 0)
            {
                ApplySuppressionStack(attacker, defender, penalty, duration, damageType);
            }

            // A Kill Box belongs to its caster, but its suppression trigger belongs to every
            // weapon attacker hitting a marked target. Use the caster as the status source so
            // Containment Net and the evasion rider remain source-owned.
            foreach (var killBox in StatusEffect.GetCreatureStatusEffects(defender)
                         .GetAllEffects()
                         .OfType<IRangedHitSuppressionSource>()
                         .Where(effect => GetIsObjectValid(effect.Source)))
            {
                var suppressionPenalty = killBox.SuppressionPenaltyPercent;
                if (suppressionPenalty <= 0)
                {
                    suppressionPenalty = Stat.GetStatAdjustment(
                        killBox.Source,
                        StatType.RangedHitSuppressionStackEvasionPenaltyPercent);
                }

                ApplySuppressionStack(
                    killBox.Source,
                    defender,
                    suppressionPenalty,
                    killBox.SuppressionDurationSeconds,
                    damageType,
                    killBox.SuppressionEvasionPenaltyAdjustment);
            }
        }

        public static bool ApplySuppressionStack(
            uint attacker,
            uint defender,
            int evasionPenaltyPercent,
            int durationSeconds,
            CombatDamageType damageType,
            int evasionPenaltyAdjustment = 0)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                durationSeconds <= 0)
            {
                return false;
            }

            var adjustedEvasionPenaltyPercent = Math.Max(
                0,
                evasionPenaltyPercent +
                Stat.GetStatAdjustment(
                    attacker,
                    StatType.SuppressionStackEvasionPenaltyPercentAdjustment) +
                evasionPenaltyAdjustment);
            if (adjustedEvasionPenaltyPercent <= 0)
                return false;

            var applied = StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new SuppressionStatusEffect(adjustedEvasionPenaltyPercent),
                durationSeconds,
                damageType);

            if (applied)
                ReconcileContainmentNetStatus(attacker, defender);

            return applied;
        }

        public static int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(target))
                return 0;

            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Count(effect => !GetIsObjectValid(source) || effect.Source == source);
        }

        public static void ReconcileContainmentNetStatus(uint source, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            ReconcileContainmentNetStatuses(target);
        }

        public static void ReconcileContainmentNetStatuses(uint target)
        {
            if (!GetIsObjectValid(target) || !_containmentNormalizationTargets.Add(target))
                return;

            try
            {
                var allEffects = StatusEffect.GetCreatureStatusEffects(target)
                    .GetAllEffects()
                    .ToList();
                var existingEffects = allEffects
                    .OfType<ContainmentNetStatusEffect>()
                    .ToList();
                var suppressionEffects = allEffects
                    .OfType<SuppressionStatusEffect>()
                    .ToList();
                var sources = suppressionEffects
                    .Select(effect => effect.Source)
                    .Concat(existingEffects.Select(effect => effect.Source))
                    .Distinct()
                    .Where(GetIsObjectValid)
                    .OrderBy(source => source)
                    .ToList();

                var remainingPenaltyPercent = ContainmentNetStatusEffect.MaximumDamagePenaltyPercent;
                var desiredAdjustments = new Dictionary<uint, int>();
                foreach (var source in sources)
                {
                    var requiredStacks = Stat.GetStatAdjustment(source, StatType.SuppressionStackDamageDealtRequiredStacks);
                    var adjustment = Stat.GetStatAdjustment(source, StatType.SuppressionStackDamageDealtPercentAdjustment);
                    if (!ContainmentNetStatusEffect.ShouldRemainActive(
                            suppressionEffects.Count(effect => effect.Source == source),
                            requiredStacks,
                            adjustment))
                        continue;

                    var desiredAdjustment = ContainmentNetStatusEffect.GetCappedDamageAdjustment(
                        adjustment,
                        remainingPenaltyPercent);
                    desiredAdjustments[source] = desiredAdjustment;
                    if (desiredAdjustment < 0)
                        remainingPenaltyPercent = Math.Max(0, remainingPenaltyPercent + desiredAdjustment);
                }

                var hadInvalidSource = existingEffects.Any(effect => !GetIsObjectValid(effect.Source));
                if (hadInvalidSource)
                {
                    StatusEffect.RemoveStatusEffect(target, typeof(ContainmentNetStatusEffect), false);
                    existingEffects.Clear();
                }

                var existingEffectsBySource = existingEffects
                    .GroupBy(effect => effect.Source)
                    .ToDictionary(group => group.Key, group => group.ToList());
                foreach (var existing in existingEffectsBySource)
                {
                    if (!desiredAdjustments.TryGetValue(existing.Key, out var desiredAdjustment) ||
                        existing.Value.Count != 1 ||
                        existing.Value[0].DamageAdjustmentPercent != desiredAdjustment)
                    {
                        StatusEffect.RemoveStatusEffect(
                            target,
                            typeof(ContainmentNetStatusEffect),
                            existing.Key,
                            false);
                    }
                }

                foreach (var desired in desiredAdjustments)
                {
                    if (existingEffectsBySource.TryGetValue(desired.Key, out var existing) &&
                        existing.Count == 1 &&
                        existing[0].DamageAdjustmentPercent == desired.Value)
                    {
                        continue;
                    }

                    StatusEffect.ApplyStatusEffect(
                        desired.Key,
                        target,
                        new ContainmentNetStatusEffect(desired.Value),
                        -1f);
                }
            }
            finally
            {
                _containmentNormalizationTargets.Remove(target);
            }
        }

        public static int ConsumeSuppressionRangedAttackAccuracyAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType))
                return 0;

            var now = Clock.UtcNow;
            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment);
            if (adjustment == 0 ||
                GetSuppressionStackCount(defender, attacker) <= 0)
            {
                RefreshOverwatchMarker(attacker, now);
                return 0;
            }

            var key = (attacker, defender);
            if (!_pendingSuppressionAbilityUses.TryGetValue(key, out var state))
            {
                RefreshOverwatchMarker(attacker, now);
                return 0;
            }

            if (state.Expiration <= now)
            {
                _pendingSuppressionAbilityUses.Remove(key);
                RefreshOverwatchMarker(attacker, now);
                return 0;
            }

            if (!HasCurrentSuppressionAbilityUseStack(attacker, defender, state.SuppressionEffectIds))
            {
                _pendingSuppressionAbilityUses.Remove(key);
                RefreshOverwatchMarker(attacker, now);
                return 0;
            }

            _pendingSuppressionAbilityUses.Remove(key);
            RefreshOverwatchMarker(attacker, now);
            if (GetIsPC(attacker))
            {
                PlayerFeedback.SendDiagnosticToPlayer(attacker, ColorToken.Combat($"Overwatch: +{adjustment}% Accuracy."));
                FloatingTextStringOnCreature(ColorToken.Combat("Overwatch"), attacker, false);
            }
            return adjustment;
        }

        public static int GetRangedAbilityLongRangeHitChanceAdjustment(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            return IsRangedAbilityLongRangeTarget(creature, defender, skillType)
                ? Stat.GetStatAdjustment(creature, StatType.RangedAbilityLongRangeHitChancePercentAdjustment)
                : 0;
        }

        public static int GetRangedAbilityLongRangeCriticalRateAdjustment(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            return IsRangedAbilityLongRangeTarget(creature, defender, skillType)
                ? Stat.GetStatAdjustment(creature, StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment)
                : 0;
        }

        public static void ReconcileOverwatchStatus(uint source)
        {
            RefreshOverwatchMarker(source, Clock.UtcNow);
        }

        private static bool HasCurrentSuppressionAbilityUseStack(
            uint attacker,
            uint defender,
            HashSet<string> suppressionEffectIds)
        {
            return StatusEffect.GetCreatureStatusEffects(defender)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Any(effect => effect.Source == attacker && suppressionEffectIds.Contains(effect.Id));
        }

        private static void RefreshOverwatchMarker(uint attacker, DateTime now)
        {
            var pendingKeys = _pendingSuppressionAbilityUses
                .Where(entry => entry.Key.Item1 == attacker)
                .Select(entry => entry.Key)
                .ToArray();

            if (!GetIsObjectValid(attacker) ||
                Stat.GetStatAdjustment(
                    attacker,
                    StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment) == 0)
            {
                foreach (var pendingKey in pendingKeys)
                {
                    _pendingSuppressionAbilityUses.Remove(pendingKey);
                }

                if (GetIsObjectValid(attacker))
                    StatusEffect.RemoveStatusEffect(attacker, typeof(OverwatchStatusEffect), false);
                return;
            }

            var latestExpiration = DateTime.MinValue;
            foreach (var pendingKey in pendingKeys)
            {
                var pending = _pendingSuppressionAbilityUses[pendingKey];
                if (pending.Expiration <= now ||
                    !HasCurrentSuppressionAbilityUseStack(
                        attacker,
                        pendingKey.Item2,
                        pending.SuppressionEffectIds))
                {
                    _pendingSuppressionAbilityUses.Remove(pendingKey);
                    continue;
                }

                if (pending.Expiration > latestExpiration)
                    latestExpiration = pending.Expiration;
            }

            StatusEffect.RemoveStatusEffect(attacker, typeof(OverwatchStatusEffect), false);
            if (latestExpiration == DateTime.MinValue)
                return;

            var durationSeconds = latestExpiration == DateTime.MaxValue
                ? -1
                : Math.Max(1, (int)Math.Ceiling((latestExpiration - now).TotalSeconds));
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new OverwatchStatusEffect(),
                durationSeconds);
        }

        private static void TrackSuppressionAbilityUse(uint activator, DateTime usedAt)
        {
            var suppressionGroups = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Where(effect => GetIsObjectValid(effect.Source))
                .GroupBy(effect => effect.Source);

            foreach (var sourceEffects in suppressionGroups)
            {
                var effects = sourceEffects.ToArray();
                var expiration = DateTime.MaxValue;
                if (effects.All(effect => effect.DurationTicks >= 0))
                {
                    var durationSeconds = effects.Max(effect => effect.DurationTicks * effect.Frequency);
                    if (durationSeconds <= 0f)
                        continue;

                    expiration = usedAt.AddSeconds(durationSeconds);
                }

                var key = (sourceEffects.Key, activator);
                var effectIds = effects.Select(effect => effect.Id).ToHashSet();
                if (_pendingSuppressionAbilityUses.TryGetValue(key, out var existingPending) &&
                    existingPending.Expiration >= expiration)
                {
                    // A later trigger must not shorten an already armed Overwatch window.
                    effectIds.UnionWith(existingPending.SuppressionEffectIds);
                    expiration = existingPending.Expiration;
                }

                _pendingSuppressionAbilityUses[key] = new SuppressionAbilityUseState
                {
                    Expiration = expiration,
                    SuppressionEffectIds = effectIds
                };

                if (Stat.GetStatAdjustment(sourceEffects.Key,
                        StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment) != 0)
                {
                    RefreshOverwatchMarker(sourceEffects.Key, usedAt);
                }
            }
        }

        private static void ForgetSuppressionState(uint creature)
        {
            var suppressionSourcesToRefresh = _pendingSuppressionAbilityUses.Keys
                .Where(x => x.Item1 == creature || x.Item2 == creature)
                .Select(x => x.Item1)
                .Distinct()
                .ToList();
            foreach (var key in _pendingSuppressionAbilityUses.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _pendingSuppressionAbilityUses.Remove(key);
            }
            foreach (var source in suppressionSourcesToRefresh)
            {
                RefreshOverwatchMarker(source, Clock.UtcNow);
            }
        }
    }
}
