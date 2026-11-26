using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Choosing and commanding the attack target: the visible creature with the most enmity, the command that sends an enemy at it,
    /// and the stale-attack, hold and move-range rules around it. The logic that used to be static in <c>Service.Enmity</c> is moved
    /// here unchanged; the tables and the attack-command memory are reached through <see cref="IEnmityService"/>. It is the listener of
    /// the leash's and stealth's retarget notices and registers itself with both at boot. Static callers reach it through
    /// <c>GameServices.EnmityAttack</c>. Calls into the unconverted hot core and feature code sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class EnmityAttackService : IEnmityAttackService, ILeashRetargetListener, IStealthRetargetListener
    {
        private readonly IEnmityService _table;
        private readonly ICombatLeash _leash;
        private readonly IActivityService _activity;
        private readonly IStealthService _stealth;
        private readonly ICompanionTargetQuery _companionControl;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatQueriesService _queries;

        public EnmityAttackService(
            IEnmityService table,
            ICombatLeash leash,
            IActivityService activity,
            IStealthService stealth,
            ICompanionTargetQuery companionControl,
            ICombatActivityService combatActivity,
            ICombatAttackTimingService attackTiming,
            ICombatQueriesService queries)
        {
            _table = table;
            _leash = leash;
            _activity = activity;
            _stealth = stealth;
            _companionControl = companionControl;
            _combatActivity = combatActivity;
            _attackTiming = attackTiming;
            _queries = queries;
        }

        private const float MinimumStaleAttackRecoverySeconds = 4.5f;
        private const float AttackMoveRangeTolerance = 0.25f;
        private const float MeleeAttackMoveThreshold = 2.25f;

        /// <summary>
        /// Gives the leash and stealth this service as the listener of their retarget notices. A reload runs boot handlers that
        /// rerun before it adopts the carried state, so both lists are full when the first notice fires.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void RegisterRetargetListeners()
        {
            _leash.RegisterRetargetListener(this);
            _stealth.RegisterRetargetListener(this);
        }

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity that the enemy can attack.
        /// Creatures hidden from the enemy by invisibility or undetected stealth keep their enmity but are skipped until
        /// they can be seen again. If no creature can be attacked, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the attack target for.</param>
        /// <returns>The visible target with the highest enmity</returns>
        public uint GetHighestEnmityAttackTarget(uint enemy)
        {
            // OrderByDescending is stable, so ties resolve to the same creature as GetHighestEnmityTarget.
            foreach (var (creature, _) in _table.GetEnmityTable(enemy).OrderByDescending(entry => entry.Value))
            {
                if (IsEligibleAttackTarget(enemy, creature))
                    return creature;
            }

            return OBJECT_INVALID;
        }

        public bool IsEligibleAttackTarget(uint enemy, uint creature)
        {
            return GetIsObjectValid(creature) && !GetIsDead(creature) &&
                   GetCurrentHitPoints(creature) > 0 && GetArea(enemy) == GetArea(creature) &&
                   !IsHiddenFrom(enemy, creature);
        }

        /// <summary>
        /// Makes every enemy with this creature on its enmity table choose its attack target again.
        /// Call after the creature becomes hidden so enemies react without waiting for their next
        /// heartbeat or combat round.
        /// </summary>
        /// <param name="creature">The creature whose enemies should re-evaluate their target.</param>
        public void ReevaluateEnemyAttackTargets(uint creature)
        {
            foreach (var enemy in _table.GetEnemiesOf(creature))
            {
                AttackHighestEnmityTarget(enemy);
            }
        }

        /// <summary>
        /// Resumes existing combat after the caller clears the action queue. The previous attack
        /// command is no longer pending, so its throttle must not delay the replacement command.
        /// </summary>
        public void ResumeAttackAfterActionsCleared(uint creature)
        {
            _table.ForgetAttackCommand(creature);
            AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see.
        /// Stops an existing chase when its last proximity target is no longer in range.
        /// Stops attacking, but keeps its enmity, while every target is hidden by invisibility or stealth.
        /// If creature did not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        public void AttackHighestEnmityTarget(uint creature)
        {
            uint target;
            var removedProximityEnmity = false;
            if (IsRegisteredCompanion(creature))
            {
                target = PeekAuthorizedTarget(creature);
            }
            else
            {
                target = GetHighestEnmityAttackTarget(creature);
                while (GetIsObjectValid(target) && ShouldRemoveStaleProximityTarget(creature, target))
                {
                    _table.RemoveProximityEnmity(target, creature);
                    removedProximityEnmity = true;
                    target = GetHighestEnmityAttackTarget(creature);
                }

                if (!GetIsObjectValid(target) && GetIsObjectValid(_table.GetHighestEnmityTarget(creature)))
                {
                    HoldForHiddenTargets(creature);
                    return;
                }
            }

            if (removedProximityEnmity && !GetIsObjectValid(target))
            {
                _leash.StopCombatAfterProximityLoss(creature);
                return;
            }

            AttackTargetIfNeeded(creature, target);
        }

        /// <summary>
        /// Every creature on the enemy's enmity table is hidden by invisibility or stealth. Drops the enemy's
        /// attack or chase so it stops striking at a target it cannot see. The enmity table is kept,
        /// so the enemy resumes once one of its targets can be seen again.
        /// </summary>
        private void HoldForHiddenTargets(uint creature)
        {
            if (_activity.IsBusy(creature) || CreatureAIRules.IsLeashEvading(creature))
                return;

            var currentAction = GetCurrentAction(creature);
            if (currentAction != ActionType.AttackObject &&
                currentAction != ActionType.MoveToPoint &&
                !GetIsObjectValid(GetAttackTarget(creature)))
            {
                return;
            }

            _table.ForgetAttackCommand(creature);
            AssignCommand(creature, () => ClearAllActions(true));
        }

        private void AttackTargetIfNeeded(uint creature, uint target)
        {
            if (!GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
                return;

            if (_leash.TryStartCombatLeashEvade(creature, target))
                return;

            // Same target - no need to switch.
            var attackTarget = GetAttackTarget(creature);
            var currentAction = GetCurrentAction(creature);
            var isBusy = _activity.IsBusy(creature);
            var shouldRecoverStaleAttack = ShouldRecoverStaleAttack(
                creature,
                attackTarget,
                target,
                currentAction);
            var hasCommand = _table.TryGetAttackCommand(creature, out var commandTime, out var queuedTarget);
            var commandIssuedAt = commandTime == default
                ? (DateTime?)null
                : commandTime;
            var pendingTarget = hasCommand
                ? queuedTarget : OBJECT_INVALID;
            var recoverySeconds = GetStaleAttackRecoverySeconds(creature);

            if (!ShouldIssueAttackCommand(
                    attackTarget,
                    target,
                    currentAction,
                    isBusy,
                    shouldRecoverStaleAttack,
                    Clock.UtcNow,
                    commandIssuedAt,
                    recoverySeconds,
                    pendingTarget))
            {
                return;
            }

            if (shouldRecoverStaleAttack)
                WriteAILog($"{GetName(creature)} recovered stale attack action against {GetName(target)}.");

            IssueAttackCommand(creature, target);
        }

        public void IssueAttackCommand(uint creature, uint target, bool clearActions = true)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
            {
                return;
            }

            if (!CanIssueAttackCommand(creature, target))
                return;

            if (_leash.TryStartCombatLeashEvade(creature, target))
                return;

            _table.RecordAttackCommand(creature, target, Clock.UtcNow);
            AssignCommand(creature, () =>
            {
                if (!_table.TryGetAttackCommand(creature, out _, out var queuedTarget) || queuedTarget != target)
                    return;
                if (_leash.TryStartCombatLeashEvade(creature, target))
                    return;

                if (clearActions)
                    ClearAllActions(true);

                if (ShouldMoveIntoAttackRange(creature, target))
                    ActionMoveToObject(target, true, GetAttackMoveRange(creature));

                ActionDoCommand(() =>
                {
                    if (!_table.TryGetAttackCommand(creature, out _, out var pendingTarget) || pendingTarget != target ||
                        !GetIsObjectValid(target) || GetIsDead(target) || GetArea(creature) != GetArea(target))
                        return;
                    if (_leash.TryStartCombatLeashEvade(creature, target))
                        return;

                    // The target may have turned invisible or slipped into stealth during the approach.
                    if (IsHiddenFrom(creature, target))
                        return;

                    ActionAttack(target);
                });
            });
        }

        private bool ShouldIssueAttackCommand(
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction,
            bool isBusy,
            bool shouldRecoverStaleAttack,
            DateTime now,
            DateTime? commandIssuedAt,
            float recoverySeconds,
            uint pendingTarget)
        {
            if (isBusy)
                return false;

            if (shouldRecoverStaleAttack)
                return true;

            // The native attack target can still describe the previous swing while
            // an already-issued switch waits for its assigned callback.
            if (pendingTarget == desiredTarget && HasRecentAttackCommand(now, commandIssuedAt, recoverySeconds))
                return false;

            if (attackTarget != OBJECT_INVALID && attackTarget != desiredTarget)
                return true;

            // An approach can have a destination before GetAttackTarget is populated.
            if (pendingTarget != OBJECT_INVALID && pendingTarget != desiredTarget)
                return true;

            if (HasRecentAttackCommand(now, commandIssuedAt, recoverySeconds))
                return false;

            return currentAction != ActionType.AttackObject;
        }

        private bool HasRecentAttackCommand(DateTime now, DateTime? commandIssuedAt, float recoverySeconds)
        {
            return commandIssuedAt != null &&
                   (now - commandIssuedAt.Value).TotalSeconds < recoverySeconds;
        }

        private bool ShouldMoveIntoAttackRange(uint creature, uint target)
        {
            if (GetIsPC(creature) ||
                !GetIsObjectValid(target))
            {
                return false;
            }

            var skillType = GetEquippedWeaponSkillType(creature);
            var moveRange = GetWeaponEngagementRange(skillType);

            return ShouldMoveIntoAttackRange(GetDistanceBetween(creature, target), skillType, moveRange);
        }

        private bool ShouldMoveIntoAttackRange(float distance, SkillType skillType, float moveRange)
        {
            var threshold = IsRangedWeaponSkill(skillType)
                ? moveRange + AttackMoveRangeTolerance
                : MeleeAttackMoveThreshold;

            return distance > threshold;
        }

        private float GetAttackMoveRange(uint creature)
        {
            var skillType = GetEquippedWeaponSkillType(creature);
            return GetWeaponEngagementRange(skillType);
        }

        private bool ShouldRemoveStaleProximityTarget(uint enemy, uint target)
        {
            return _table.HasOnlyProximityEnmity(target, enemy) &&
                   !CreatureAIRules.IsInAggroRange(enemy, target);
        }

        private bool ShouldRecoverStaleAttack(
            uint creature,
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction)
        {
            _table.TryGetAttackCommand(creature, out var commandTime, out _);
            var commandIssuedAt = commandTime == default
                ? (DateTime?)null
                : commandTime;
            var recoverySeconds = GetStaleAttackRecoverySeconds(creature);

            return ShouldRecoverStaleAttack(
                attackTarget,
                desiredTarget,
                currentAction,
                Clock.UtcNow,
                commandIssuedAt,
                HasRecentAttackActivity(creature, recoverySeconds),
                recoverySeconds);
        }

        private bool ShouldRecoverStaleAttack(
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction,
            DateTime now,
            DateTime? commandIssuedAt,
            bool hasRecentAttack,
            float recoverySeconds)
        {
            if (attackTarget != desiredTarget ||
                currentAction != ActionType.AttackObject ||
                hasRecentAttack)
            {
                return false;
            }

            if (commandIssuedAt == null)
                return true;

            return (now - commandIssuedAt.Value).TotalSeconds >= recoverySeconds;
        }

        private float GetStaleAttackRecoverySeconds(uint creature)
        {
            var calculatedDelay = CalculateAttackDelay(creature);
            var effectiveDelay = CalculateEffectiveAttackDelay(calculatedDelay);

            return GetStaleAttackRecoverySeconds(effectiveDelay);
        }

        private float GetStaleAttackRecoverySeconds(int effectiveDelayMilliseconds)
        {
            // Attacks arrive in swings; fast delays resolve multiple attacks per swing,
            // so staleness is measured against the swing cadence rather than the per-attack delay.
            var swingDelaySeconds = CalculateAttackSwingDelay(effectiveDelayMilliseconds) / 1000f;

            return Math.Max(MinimumStaleAttackRecoverySeconds, swingDelaySeconds * 2f + 1f);
        }

        /// <summary>The unconverted <c>Stealth</c> service.</summary>
        protected virtual bool IsHiddenFrom(uint observer, uint target) => _stealth.IsHiddenFrom(observer, target);

        /// <summary>The companion-control service.</summary>
        protected virtual bool IsRegisteredCompanion(uint creature) => _companionControl.IsRegisteredCompanion(creature);

        /// <summary>The companion-control service.</summary>
        protected virtual uint PeekAuthorizedTarget(uint companion) => _companionControl.PeekAuthorizedTarget(companion);

        /// <summary>The companion-control service.</summary>
        protected virtual bool CanIssueAttackCommand(uint companion, uint target) =>
            _companionControl.CanIssueAttackCommand(companion, target);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual SkillType GetEquippedWeaponSkillType(uint creature) => _queries.GetEquippedWeaponSkillType(creature);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual float GetWeaponEngagementRange(SkillType skillType) => CombatRules.GetWeaponEngagementRange(skillType);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual bool IsRangedWeaponSkill(SkillType skillType) => CombatRules.IsRangedWeaponSkill(skillType);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual bool HasRecentAttackActivity(uint creature, float windowSeconds) =>
            _combatActivity.HasRecentAttackActivity(creature, windowSeconds);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateAttackDelay(uint attacker) => _attackTiming.CalculateAttackDelay(attacker);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds) =>
            CombatRules.CalculateEffectiveAttackDelay(attackerDelayMilliseconds);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateAttackSwingDelay(int effectiveDelayMilliseconds) =>
            CombatRules.CalculateAttackSwingDelay(effectiveDelayMilliseconds);

        /// <summary>The unconverted <c>Log</c> sink.</summary>
        protected virtual void WriteAILog(string details) => Log.Write(LogGroup.AI, details);
    }
}
