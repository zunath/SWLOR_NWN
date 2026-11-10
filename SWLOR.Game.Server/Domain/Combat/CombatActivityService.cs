using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The combat activity records of <c>Combat</c>: when a creature last attacked, used a hostile ability, took or
    /// dealt damage and entered combat, the First Strike stacks and stealth opening window those records drive, and the
    /// idle-readiness markers and idle skill ability bonuses that read them. The ten tables are this instance's own
    /// fields. <c>Combat</c> keeps the heartbeat purge and the login refresh as its handlers and forwards them here.
    /// </summary>
    public class CombatActivityService : ICombatActivityService
    {
        private readonly Dictionary<(uint, uint), DateTime> _recentDamageTargets = new();
        private readonly Dictionary<uint, DateTime> _recentDamageTaken = new();
        private readonly Dictionary<uint, DateTime> _lastCombatActivity = new();
        private readonly Dictionary<uint, DateTime> _lastHostileAbilityAttemptActivity = new();
        private readonly Dictionary<uint, DateTime> _lastHostileIncomingActivity = new();
        private readonly HashSet<uint> _firstCombatAttackConsumed = new();
        // Short-window activity queries must preserve the timestamps used by idle-readiness callbacks.
        private readonly Dictionary<uint, DateTime> _lastAttackActivity = new();
        private readonly Dictionary<uint, DateTime> _lastCombatAbilityUse = new();
        private readonly Dictionary<uint, TargetHitSequenceState> _firstHostileAbilityHitCounts = new();
        private readonly Dictionary<uint, DateTime> _stealthOpeningWindows = new();

        private readonly IdleSkillAbilityStatChannel[] _idleSkillAbilityStatChannels =
        {
            new(
                StatType.IdleSkillAbilitySkillType,
                StatType.IdleSkillAbilityRequiredIdleSeconds,
                StatType.IdleSkillAbilityDamageBonus,
                StatType.IdleSkillAbilityHitChancePercentAdjustment,
                StatType.IdleSkillAbilityCriticalDamagePercentAdjustment),
            new(
                StatType.AlternateIdleSkillAbilitySkillType,
                StatType.AlternateIdleSkillAbilityRequiredIdleSeconds,
                StatType.AlternateIdleSkillAbilityDamageBonus,
                StatType.AlternateIdleSkillAbilityHitChancePercentAdjustment,
                StatType.AlternateIdleSkillAbilityCriticalDamagePercentAdjustment),
        };

        private readonly record struct IdleSkillAbilityStatChannel(
            StatType SkillType,
            StatType RequiredIdleSeconds,
            StatType DamageBonus,
            StatType HitChancePercentAdjustment,
            StatType CriticalDamagePercentAdjustment);

        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
            public DateTime? RechargeAvailableAt { get; init; }
        }

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatSuppressionService _suppression;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly IAbilityService _ability;
        private readonly IActivityService _activity;

        public CombatActivityService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatSuppressionService suppression,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IPlayerFeedbackService playerFeedback,
            IAbilityService ability,
            IActivityService activity)
        {
            _queries = queries;
            _resources = resources;
            _suppression = suppression;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _playerFeedback = playerFeedback;
            _ability = ability;
            _activity = activity;
        }

        /// <summary>The last time each attacker damaged each defender; the hit effects and damage taken parts read it too.</summary>
        public Dictionary<(uint, uint), DateTime> RecentDamageTargets => _recentDamageTargets;

        public void ApplyFirstCombatAttackStaminaRestore(uint attacker)
        {
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.FirstCombatAttackStaminaRestore);
            var cooldownSeconds = _stat.GetStatAdjustment(attacker, StatType.FirstCombatAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || cooldownSeconds <= 0)
                return;

            if (!_firstCombatAttackConsumed.Add(attacker))
                return;

            if (!_resources.TryUseStatTrigger(attacker, StatType.FirstCombatAttackStaminaRestore, cooldownSeconds))
                return;

            _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.FirstCombatAttackStaminaRestore, staminaRestore);
        }

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public void PurgeInvalidOffensiveActivity()
        {
            // Area despawns destroy creatures without firing death or player-exit events.
            var invalidCreatures = _lastAttackActivity.Keys
                .Concat(_lastCombatAbilityUse.Keys)
                .Distinct()
                .Where(creature => !GetIsObjectValid(creature))
                .ToList();
            foreach (var creature in invalidCreatures)
            {
                _lastAttackActivity.Remove(creature);
                _lastCombatAbilityUse.Remove(creature);
            }
        }

        public void TrackRecentDamageTarget(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            _recentDamageTargets[(attacker, defender)] = Clock.UtcNow;
        }

        public void TrackRecentDamageTaken(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _recentDamageTaken[creature] = Clock.UtcNow;
        }

        public bool HasRecentDamageTaken(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_recentDamageTaken.TryGetValue(creature, out var lastDamaged))
                return false;

            var isRecent = (Clock.UtcNow - lastDamaged).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDamageTaken.Remove(creature);

            return isRecent;
        }

        public void TrackCombatActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var now = Clock.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            _lastCombatActivity[creature] = now;
        }

        public void TrackAttackActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _lastAttackActivity[creature] = Clock.UtcNow;
            _statusEffect.RemoveStatusEffect(creature, typeof(OpeningAttackReadyStatusEffect), false);
            _statusEffect.RemoveStatusEffect(creature, typeof(IdleSkillAbilityReadyStatusEffect), false);
            TrackCombatActivity(creature);
            ScheduleIdleReadinessRefresh(creature);
        }

        public void TrackHostileAbilityActivity(uint creature, bool countAsCompletedOffensiveActivity = false)
        {
            if (!GetIsObjectValid(creature))
                return;

            var now = Clock.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            // Keep cast attempts separate from landed combat activity. Opening-hit riders such as
            // Venatic Recovery must still observe the previous landed-combat timestamp.
            _lastHostileAbilityAttemptActivity[creature] = now;
            _statusEffect.RemoveStatusEffect(creature, typeof(OpeningAttackReadyStatusEffect), false);
            _statusEffect.RemoveStatusEffect(creature, typeof(IdleSkillAbilityReadyStatusEffect), false);
            if (countAsCompletedOffensiveActivity)
            {
                _lastCombatAbilityUse[creature] = now;
            }
            // Re-arm readiness even if the cast is cancelled before an impact completes. A
            // completed impact schedules again after its completed-activity timestamp is stored.
            ScheduleIdleReadinessRefresh(creature);
        }

        public void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(attacker) ||
                !GetIsReactionTypeHostile(attacker, creature))
                return;

            var now = Clock.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            // Incoming hostile actions start combat for First Strike visibility without consuming the
            // landed-opening timestamp that Venatic Recovery reads when the defender retaliates.
            _lastHostileIncomingActivity[creature] = now;
        }

        private void ReportCombatEntryIfNeeded(uint creature, DateTime now)
        {
            if (HasRecentCombatEntryActivity(creature, now))
                return;

            _firstCombatAttackConsumed.Remove(creature);
            ReportFirstStrikeCombatEntry(creature, now);
        }

        public bool HasRecentHostileCombatActivity(uint creature) =>
            GetIsObjectValid(creature) && (GetIsInCombat(creature) || HasRecentCombatEntryActivity(creature, Clock.UtcNow));

        private bool HasRecentCombatEntryActivity(uint creature, DateTime now)
        {
            return (_lastCombatActivity.TryGetValue(creature, out var lastCombatActivity) &&
                    (now - lastCombatActivity).TotalSeconds <= 30) ||
                (_lastHostileAbilityAttemptActivity.TryGetValue(creature, out var lastHostileAbilityAttempt) &&
                    (now - lastHostileAbilityAttempt).TotalSeconds <= 30) ||
                (_lastHostileIncomingActivity.TryGetValue(creature, out var lastHostileIncomingActivity) &&
                    (now - lastHostileIncomingActivity).TotalSeconds <= 30);
        }

        public void TrackStealthOpeningWindow(uint creature)
        {
            if (!GetIsObjectValid(creature) || !_queries.IsStealthedOrInvisible(creature))
                return;

            _stealthOpeningWindows[creature] = Clock.UtcNow.AddSeconds(6);
        }

        private bool ConsumeStealthOpeningWindow(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            if (_queries.IsStealthedOrInvisible(creature))
                return true;

            if (!_stealthOpeningWindows.TryGetValue(creature, out var expiresAt))
                return false;

            var isActive = expiresAt >= Clock.UtcNow;
            _stealthOpeningWindows.Remove(creature);
            return isActive;
        }

        public bool HasRecentAttackActivity(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_lastAttackActivity.TryGetValue(creature, out var lastAttack))
                return false;

            return (Clock.UtcNow - lastAttack).TotalSeconds <= windowSeconds;
        }

        public DateTime GetLastOffensiveActivityAt(uint creature)
        {
            _lastAttackActivity.TryGetValue(creature, out var lastAttack);
            _lastHostileAbilityAttemptActivity.TryGetValue(creature, out var lastHostileAbility);
            return lastAttack > lastHostileAbility
                ? lastAttack
                : lastHostileAbility;
        }

        public DateTime GetLastCompletedOffensiveActivityAt(uint creature)
        {
            _lastAttackActivity.TryGetValue(creature, out var lastAttack);
            _lastCombatAbilityUse.TryGetValue(creature, out var lastAbility);
            return lastAttack > lastAbility ? lastAttack : lastAbility;
        }

        public bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender || windowSeconds <= 0f)
                return false;

            if (!_recentDamageTargets.TryGetValue((attacker, defender), out var lastDamaged))
                return false;

            var isRecent = (Clock.UtcNow - lastDamaged).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDamageTargets.Remove((attacker, defender));

            return isRecent;
        }

        public int GetDirectDamageToStatusCategoryOrStealthBonus(uint attacker, uint defender)
        {
            var bonus = _stat.GetStatAdjustment(attacker, StatType.DirectDamageToStatusCategoryOrStealthBonus);
            if (bonus == 0)
                return 0;

            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.DirectDamageToStatusCategoryOrStealthBonusCategory));
            if (category != 0 && _queries.TargetHasAnyStatusEffectCategory(defender, category))
                return bonus;

            return ConsumeStealthOpeningWindow(attacker)
                ? bonus
                : 0;
        }

        public int GetFirstHostileAbilityHitDamageBonus(uint attacker, AbilityDetail ability)
        {
            var bonus = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            var maximumCount = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            if (bonus <= 0 || maximumCount <= 0 || ability?.IsHostileAbility != true)
                return 0;

            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, bonus, out _);
            return state.Count < maximumCount
                ? bonus
                : 0;
        }

        public void ApplyFirstHostileAbilityHitCount(uint attacker, AbilityDetail ability)
        {
            var maximumCount = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            if (maximumCount <= 0 || ability?.IsHostileAbility != true)
                return;

            var damageBonus = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, damageBonus, out _);

            var now = Clock.UtcNow;
            if (_ability.IsAreaImpact(attacker, ability) &&
                (now - state.LastHit).TotalSeconds <= 1)
            {
                return;
            }

            var currentCount = state?.Count ?? 0;
            if (currentCount >= maximumCount)
                return;

            var newCount = Math.Min(maximumCount, currentCount + 1);
            var rechargeAvailableAt = state?.RechargeAvailableAt;
            if (newCount >= maximumCount)
            {
                // The final stack was just consumed; open the recharge window.
                var cooldownSeconds = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitCooldownSeconds);
                rechargeAvailableAt = cooldownSeconds > 0
                    ? now.AddSeconds(cooldownSeconds)
                    : null;
            }

            _firstHostileAbilityHitCounts[attacker] = new TargetHitSequenceState
            {
                Count = newCount,
                LastHit = now,
                RechargeAvailableAt = rechargeAvailableAt
            };

            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike stack consumed: Attacker={Attacker} Count={Count} MaximumCount={MaximumCount} DamageBonus={DamageBonus} RechargeAvailableAt={RechargeAvailableAt}",
                attacker,
                newCount,
                maximumCount,
                damageBonus,
                rechargeAvailableAt);

            if (damageBonus > 0 && GetIsPC(attacker))
            {
                var remaining = maximumCount - newCount;
                var stackLabel = remaining == 1 ? "stack" : "stacks";
                var cooldownSeconds = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitCooldownSeconds);
                var rechargeText = remaining == 0
                    ? cooldownSeconds > 0
                        ? $"; recharges in {cooldownSeconds} seconds"
                        : "; recharges after combat"
                    : string.Empty;
                var feedback = ColorToken.Combat(
                    $"First Strike deals +{damageBonus} DMG ({remaining} {stackLabel} remaining{rechargeText}).");

                SendMessageToPC(attacker, feedback);
                _playerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"First Strike +{damageBonus} DMG ({remaining} {stackLabel} remaining)"),
                    attacker,
                    false);
            }
        }

        private TargetHitSequenceState EnsureFirstHostileAbilityHitState(
            uint attacker,
            int maximumCount,
            int damageBonus,
            out bool becameReady)
        {
            RechargeFirstHostileAbilityHitStacks(attacker, maximumCount);
            if (_firstHostileAbilityHitCounts.TryGetValue(attacker, out var state))
            {
                becameReady = false;
                return state;
            }

            state = new TargetHitSequenceState
            {
                Count = 0,
                LastHit = DateTime.MinValue
            };
            _firstHostileAbilityHitCounts[attacker] = state;
            becameReady = true;

            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike ready: Attacker={Attacker} Count={Count} MaximumCount={MaximumCount} DamageBonus={DamageBonus}",
                attacker,
                state.Count,
                maximumCount,
                damageBonus);

            if (damageBonus > 0 && GetIsPC(attacker))
            {
                var stackLabel = maximumCount == 1 ? "stack" : "stacks";
                var feedback = ColorToken.Combat(
                    $"First Strike ready: {maximumCount} {stackLabel} (+{damageBonus} DMG each).");
                SendMessageToPC(attacker, feedback);
                _playerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"First Strike ready ({maximumCount} {stackLabel})"),
                    attacker,
                    false);
            }

            return state;
        }

        private void ReportFirstStrikeCombatEntry(uint attacker, DateTime now)
        {
            var maximumCount = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            var damageBonus = _stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            if (maximumCount <= 0 || damageBonus <= 0)
                return;

            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, damageBonus, out var becameReady);
            if (becameReady ||
                state.Count < maximumCount ||
                state.RechargeAvailableAt == null ||
                !GetIsPC(attacker))
            {
                return;
            }

            var remainingSeconds = Math.Max(1, (int)Math.Ceiling((state.RechargeAvailableAt.Value - now).TotalSeconds));
            SendMessageToPC(
                attacker,
                ColorToken.Combat($"First Strike is recharging ({remainingSeconds} seconds remaining)."));
        }

        private void RechargeFirstHostileAbilityHitStacks(uint attacker, int maximumCount)
        {
            if (!_firstHostileAbilityHitCounts.TryGetValue(attacker, out var state))
                return;

            // Exhausted stacks recharge strictly on the cooldown timer, even across separate engagements.
            if (state.Count >= maximumCount && state.RechargeAvailableAt != null)
            {
                if (Clock.UtcNow >= state.RechargeAvailableAt.Value)
                {
                    _firstHostileAbilityHitCounts.Remove(attacker);
                    Log.WriteStructured(
                        LogGroup.Attack,
                        "First Strike recharged: Attacker={Attacker} PreviousCount={PreviousCount} MaximumCount={MaximumCount} RechargeAvailableAt={RechargeAvailableAt}",
                        attacker,
                        state.Count,
                        maximumCount,
                        state.RechargeAvailableAt.Value);
                }

                return;
            }

            // With stacks remaining, refresh to a full set once the wielder drops out of combat so each new engagement opens with all stacks.
            if (HasRecentCombatEntryActivity(attacker, Clock.UtcNow))
            {
                return;
            }

            _firstHostileAbilityHitCounts.Remove(attacker);
            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike reset after combat: Attacker={Attacker} PreviousCount={PreviousCount} MaximumCount={MaximumCount}",
                attacker,
                state.Count,
                maximumCount);
        }

        public void TrackCombatAbilityUse(uint activator, AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var skillType = _queries.GetAbilitySkillType(activator, ability);
            if (skillType == SkillType.Invalid)
                return;

            var now = Clock.UtcNow;
            _suppression.TrackSuppressionAbilityUse(activator, now);

            if (!ability.IsHostileAbility)
                return;

            _lastCombatAbilityUse[activator] = now;
            ScheduleIdleReadinessRefresh(activator);
        }

        public bool HasRecentCombatAbilityUse(uint activator, float windowSeconds)
        {
            if (!_lastCombatAbilityUse.TryGetValue(activator, out var lastUse))
                return false;

            return (Clock.UtcNow - lastUse).TotalSeconds <= windowSeconds;
        }

        public bool IsUsingAbility(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (_activity.GetBusyType(creature) == ActivityStatusType.AbilityActivation ||
                    HasRecentCombatAbilityUse(creature, 2f));
        }

        public (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return (0, 0, 0);

            var activationSkill = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AbilityActivationIdleSkillType,
                StatType.AbilityActivationIdleSkillType));
            if (SkillTypeMatches(skillType, activationSkill))
            {
                return (
                    _temporaryStatModifier.GetStatAdjustment(activator, StatType.AbilityActivationIdleDamageBonus,
                        StatType.AbilityActivationIdleSkillType),
                    _temporaryStatModifier.GetStatAdjustment(activator, StatType.AbilityActivationIdleHitChancePercentAdjustment,
                        StatType.AbilityActivationIdleSkillType),
                    _temporaryStatModifier.GetStatAdjustment(activator, StatType.AbilityActivationIdleCriticalDamagePercentAdjustment,
                        StatType.AbilityActivationIdleSkillType));
            }

            return CalculateIdleSkillAbilityBonuses(activator, skillType);
        }

        private (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) CalculateIdleSkillAbilityBonuses(uint activator, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return (0, 0, 0);

            var lastOffensiveUse = GetLastCompletedOffensiveActivityAt(activator);
            var elapsedSeconds = lastOffensiveUse == default
                ? double.MaxValue
                : (Clock.UtcNow - lastOffensiveUse).TotalSeconds;
            var damageBonus = 0;
            var accuracyBonus = 0;
            var criticalDamageBonus = 0;
            var hasReadyChannel = false;

            foreach (var channel in _idleSkillAbilityStatChannels)
            {
                var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(activator, channel.SkillType));
                var requiredIdleSeconds = _stat.GetStatAdjustment(activator, channel.RequiredIdleSeconds);
                if (!SkillTypeMatches(skillType, requiredSkillType) ||
                    requiredIdleSeconds <= 0 ||
                    elapsedSeconds < requiredIdleSeconds)
                    continue;

                hasReadyChannel = true;
                damageBonus += _stat.GetStatAdjustment(activator, channel.DamageBonus);
                accuracyBonus += _stat.GetStatAdjustment(activator, channel.HitChancePercentAdjustment);
                criticalDamageBonus += _stat.GetStatAdjustment(activator, channel.CriticalDamagePercentAdjustment);
            }

            if (!hasReadyChannel)
                return (0, 0, 0);

            return (damageBonus, accuracyBonus, criticalDamageBonus);
        }

        public void StoreAbilityActivationIdleBonuses(uint activator, SkillType skillType)
        {
            ClearAbilityActivationIdleBonuses(activator);
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var bonuses = CalculateIdleSkillAbilityBonuses(activator, skillType);
            _temporaryStatModifier.Replace(activator, StatType.AbilityActivationIdleSkillType,
                (int)skillType, 30, StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Replace(activator, StatType.AbilityActivationIdleDamageBonus,
                bonuses.DamageBonus, 30, StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Replace(activator, StatType.AbilityActivationIdleHitChancePercentAdjustment,
                bonuses.HitChancePercentAdjustment, 30, StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Replace(activator, StatType.AbilityActivationIdleCriticalDamagePercentAdjustment,
                bonuses.CriticalDamagePercentAdjustment, 30, StatType.AbilityActivationIdleSkillType);
        }

        public void ClearAbilityActivationIdleBonuses(uint activator)
        {
            _temporaryStatModifier.Consume(activator, StatType.AbilityActivationIdleDamageBonus,
                StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Consume(activator, StatType.AbilityActivationIdleHitChancePercentAdjustment,
                StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Consume(activator, StatType.AbilityActivationIdleCriticalDamagePercentAdjustment,
                StatType.AbilityActivationIdleSkillType);
            _temporaryStatModifier.Consume(activator, StatType.AbilityActivationIdleSkillType,
                StatType.AbilityActivationIdleSkillType);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void RestoreIdleReadinessOnPlayerEnter()
        {
            var creature = GetEnteringObject();
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            DelayCommand(0f, () =>
            {
                RefreshIdleReadiness(creature);
                ScheduleIdleReadinessRefresh(creature);
            });
        }

        private void RefreshIdleReadiness(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var lastActivity = GetLastCompletedOffensiveActivityAt(creature);
            var elapsed = lastActivity == default
                ? double.MaxValue
                : (Clock.UtcNow - lastActivity).TotalSeconds;

            var openingSeconds = _stat.GetStatAdjustment(creature, StatType.OpeningAutoAttackIdleSeconds);
            if (openingSeconds > 0 && elapsed >= openingSeconds &&
                !_statusEffect.HasStatusEffect(creature, typeof(OpeningAttackReadyStatusEffect)))
            {
                _statusEffect.ApplyStatusEffect(creature, creature, new OpeningAttackReadyStatusEffect(), -1);
                if (GetIsPC(creature))
                    FloatingTextStringOnCreature(ColorToken.Combat("Opening Attack Ready"), creature, false);
            }

            var hasReadyIdleSkillChannel = _idleSkillAbilityStatChannels.Any(channel =>
            {
                var idleSkill = GetSkillTypeFromStat(_stat.GetStatAdjustment(creature, channel.SkillType));
                var idleSeconds = _stat.GetStatAdjustment(creature, channel.RequiredIdleSeconds);
                return idleSkill != SkillType.Invalid && idleSeconds > 0 && elapsed >= idleSeconds;
            });
            if (hasReadyIdleSkillChannel &&
                !_statusEffect.HasStatusEffect(creature, typeof(IdleSkillAbilityReadyStatusEffect)))
            {
                _statusEffect.ApplyStatusEffect(creature, creature, new IdleSkillAbilityReadyStatusEffect(), -1);
                if (GetIsPC(creature))
                    FloatingTextStringOnCreature(ColorToken.Combat("Idle Skill Ability Ready"), creature, false);
            }
        }

        public void ScheduleIdleReadinessRefresh(uint creature)
        {
            var openingSeconds = _stat.GetStatAdjustment(creature, StatType.OpeningAutoAttackIdleSeconds);
            var scheduledActivity = GetLastCompletedOffensiveActivityAt(creature);
            var delays = _idleSkillAbilityStatChannels
                .Select(channel => _stat.GetStatAdjustment(creature, channel.RequiredIdleSeconds))
                .Append(openingSeconds)
                .Where(value => value > 0)
                .Distinct();
            foreach (var delay in delays)
            {
                void RefreshWhenReady()
                {
                    if (!GetIsObjectValid(creature) || GetLastCompletedOffensiveActivityAt(creature) != scheduledActivity)
                        return;

                    var remaining = scheduledActivity == default
                        ? 0
                        : delay - (Clock.UtcNow - scheduledActivity).TotalSeconds;
                    if (remaining > 0)
                    {
                        DelayCommand((float)Math.Max(0.1, remaining), RefreshWhenReady);
                        return;
                    }

                    RefreshIdleReadiness(creature);
                }

                // Native attack hooks may run without a current script object. Give the
                // callback a valid owner and recheck the wall-clock deadline when it fires.
                AssignCommand(creature, () => DelayCommand(delay, RefreshWhenReady));
            }
        }

        public bool HasConfiguredIdleSkillAbilityChannel(uint creature)
        {
            return _idleSkillAbilityStatChannels.Any(channel =>
                GetSkillTypeFromStat(_stat.GetStatAdjustment(creature, channel.SkillType)) != SkillType.Invalid &&
                _stat.GetStatAdjustment(creature, channel.RequiredIdleSeconds) > 0);
        }

        public int GetIdleAbilityHitChanceAdjustment(uint activator, SkillType skillType)
        {
            return GetIdleSkillAbilityBonuses(activator, skillType).HitChancePercentAdjustment;
        }

        public void ForgetActivityState(uint creature)
        {
            foreach (var key in _recentDamageTargets.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _recentDamageTargets.Remove(key);
            }
            _recentDamageTaken.Remove(creature);
            _lastCombatActivity.Remove(creature);
            _lastHostileAbilityAttemptActivity.Remove(creature);
            _lastHostileIncomingActivity.Remove(creature);
            _firstCombatAttackConsumed.Remove(creature);
            _lastAttackActivity.Remove(creature);
            _lastCombatAbilityUse.Remove(creature);
        }

        public void ForgetOpeningActivityState(uint creature)
        {
            _firstHostileAbilityHitCounts.Remove(creature);
            _stealthOpeningWindows.Remove(creature);
        }
    }
}
