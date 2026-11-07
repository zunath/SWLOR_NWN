using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The Suppression stacks of <c>Combat</c>: the stacks weapon hits and auto-attacks apply, the Containment Net
    /// statuses they keep in step (with the re-entrancy guard of that normalisation), and the Overwatch window a
    /// suppressed creature's ability use opens for its suppressor. Both tables are this instance's own fields.
    /// </summary>
    public class CombatSuppressionService : ICombatSuppressionService
    {
        private readonly Dictionary<(uint, uint), SuppressionAbilityUseState> _pendingSuppressionAbilityUses = new();
        private readonly HashSet<uint> _containmentNormalizationTargets = new();

        private readonly ICombatQueriesService _queries;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IPlayerFeedbackService _playerFeedback;

        public CombatSuppressionService(
            ICombatQueriesService queries,
            IStatService stat,
            IStatusEffectService statusEffect,
            IPlayerFeedbackService playerFeedback)
        {
            _queries = queries;
            _stat = stat;
            _statusEffect = statusEffect;
            _playerFeedback = playerFeedback;
        }

        private sealed class SuppressionAbilityUseState
        {
            public DateTime Expiration { get; init; }
            public HashSet<string> SuppressionEffectIds { get; init; } = new();
        }

        public void ApplyAutoAttackSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponSkillType(skillType))
                return;

            var chance = _stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackChance);
            var duration = _stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var applied = ApplySuppressionStack(
                attacker,
                defender,
                _stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);

            if (applied && GetIsPC(attacker))
            {
                SendMessageToPC(attacker, ColorToken.Combat("Pinning Fire: Suppression applied."));
                _playerFeedback.ShowDiagnosticFloatingText(ColorToken.Combat("Pinning Fire"), attacker, false);
            }
        }

        public void ApplyRangedHitSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponSkillType(skillType))
                return;

            var duration = _stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackDurationSeconds);
            var penalty = _stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackEvasionPenaltyPercent);
            if (duration > 0)
            {
                ApplySuppressionStack(attacker, defender, penalty, duration, damageType);
            }

            // A Kill Box belongs to its caster, but its suppression trigger belongs to every
            // weapon attacker hitting a marked target. Use the caster as the status source so
            // Containment Net and the evasion rider remain source-owned.
            foreach (var killBox in _statusEffect.GetCreatureStatusEffects(defender)
                         .GetAllEffects()
                         .OfType<IRangedHitSuppressionSource>()
                         .Where(effect => GetIsObjectValid(effect.Source)))
            {
                var suppressionPenalty = killBox.SuppressionPenaltyPercent;
                if (suppressionPenalty <= 0)
                {
                    suppressionPenalty = _stat.GetStatAdjustment(
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

        public bool ApplySuppressionStack(
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
                _stat.GetStatAdjustment(
                    attacker,
                    StatType.SuppressionStackEvasionPenaltyPercentAdjustment) +
                evasionPenaltyAdjustment);
            if (adjustedEvasionPenaltyPercent <= 0)
                return false;

            var applied = _statusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new SuppressionStatusEffect(adjustedEvasionPenaltyPercent),
                durationSeconds,
                damageType);

            if (applied)
                ReconcileContainmentNetStatus(attacker, defender);

            return applied;
        }

        public int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(target))
                return 0;

            return _statusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Count(effect => !GetIsObjectValid(source) || effect.Source == source);
        }

        public void ReconcileContainmentNetStatus(uint source, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            ReconcileContainmentNetStatuses(target);
        }

        public void ReconcileContainmentNetStatuses(uint target)
        {
            if (!GetIsObjectValid(target) || !_containmentNormalizationTargets.Add(target))
                return;

            try
            {
                var allEffects = _statusEffect.GetCreatureStatusEffects(target)
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
                    var requiredStacks = _stat.GetStatAdjustment(source, StatType.SuppressionStackDamageDealtRequiredStacks);
                    var adjustment = _stat.GetStatAdjustment(source, StatType.SuppressionStackDamageDealtPercentAdjustment);
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
                    _statusEffect.RemoveStatusEffect(target, typeof(ContainmentNetStatusEffect), false);
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
                        _statusEffect.RemoveStatusEffect(
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

                    _statusEffect.ApplyStatusEffect(
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

        public int ConsumeSuppressionRangedAttackAccuracyAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType))
                return 0;

            var now = Clock.UtcNow;
            var adjustment = _stat.GetStatAdjustment(
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
                _playerFeedback.SendDiagnosticToPlayer(attacker, ColorToken.Combat($"Overwatch: +{adjustment}% Accuracy."));
                FloatingTextStringOnCreature(ColorToken.Combat("Overwatch"), attacker, false);
            }
            return adjustment;
        }

        public int GetRangedAbilityLongRangeHitChanceAdjustment(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            return _queries.IsRangedAbilityLongRangeTarget(creature, defender, skillType)
                ? _stat.GetStatAdjustment(creature, StatType.RangedAbilityLongRangeHitChancePercentAdjustment)
                : 0;
        }

        public int GetRangedAbilityLongRangeCriticalRateAdjustment(
            uint creature,
            uint defender,
            SkillType skillType)
        {
            return _queries.IsRangedAbilityLongRangeTarget(creature, defender, skillType)
                ? _stat.GetStatAdjustment(creature, StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment)
                : 0;
        }

        public void ReconcileOverwatchStatus(uint source)
        {
            RefreshOverwatchMarker(source, Clock.UtcNow);
        }

        private bool HasCurrentSuppressionAbilityUseStack(
            uint attacker,
            uint defender,
            HashSet<string> suppressionEffectIds)
        {
            return _statusEffect.GetCreatureStatusEffects(defender)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Any(effect => effect.Source == attacker && suppressionEffectIds.Contains(effect.Id));
        }

        private void RefreshOverwatchMarker(uint attacker, DateTime now)
        {
            var pendingKeys = _pendingSuppressionAbilityUses
                .Where(entry => entry.Key.Item1 == attacker)
                .Select(entry => entry.Key)
                .ToArray();

            if (!GetIsObjectValid(attacker) ||
                _stat.GetStatAdjustment(
                    attacker,
                    StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment) == 0)
            {
                foreach (var pendingKey in pendingKeys)
                {
                    _pendingSuppressionAbilityUses.Remove(pendingKey);
                }

                if (GetIsObjectValid(attacker))
                    _statusEffect.RemoveStatusEffect(attacker, typeof(OverwatchStatusEffect), false);
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

            _statusEffect.RemoveStatusEffect(attacker, typeof(OverwatchStatusEffect), false);
            if (latestExpiration == DateTime.MinValue)
                return;

            var durationSeconds = latestExpiration == DateTime.MaxValue
                ? -1
                : Math.Max(1, (int)Math.Ceiling((latestExpiration - now).TotalSeconds));
            _statusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new OverwatchStatusEffect(),
                durationSeconds);
        }

        public void TrackSuppressionAbilityUse(uint activator, DateTime usedAt)
        {
            var suppressionGroups = _statusEffect.GetCreatureStatusEffects(activator)
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

                if (_stat.GetStatAdjustment(sourceEffects.Key,
                        StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment) != 0)
                {
                    RefreshOverwatchMarker(sourceEffects.Key, usedAt);
                }
            }
        }

        public void ForgetSuppressionState(uint creature)
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
