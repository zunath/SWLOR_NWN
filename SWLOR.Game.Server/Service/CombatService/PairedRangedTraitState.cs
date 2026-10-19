using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Deterministic per-attacker state. Callers provide only eligible ordinary rolls, direct
    /// hostile damage and accuracy-versus-evasion misses; no perk identities enter this class.
    /// </summary>
    public sealed class PairedRangedTraitState
    {
        private readonly HashSet<long> _attempts = new();
        private readonly Queue<long> _attemptOrder = new();
        private readonly HashSet<long> _hits = new();
        private readonly Queue<long> _hitOrder = new();
        private readonly HashSet<Guid> _completedActivations = new();
        private readonly Queue<Guid> _activationOrder = new();
        private readonly HashSet<Guid> _committedActivations = new();
        private readonly Queue<Guid> _commitOrder = new();
        private readonly List<long> _momentumCharges = new();
        private long _nextMomentumChargeId;
        private long _movementAccuracyChargeId;
        private int _stateGeneration;
        private int _ordinaryHitCount;
        private int _sameTargetHits;
        private uint _ordinaryTarget;
        private DateTime _restoreReady;
        private DateTime _damageCostGrantReady;
        private DateTime _movementAccuracyGrantReady;
        private DateTime _momentumRestoreReady;
        private DateTime _evasionGrantReady;
        private int _previousExtendedDuration;

        public int Momentum => _momentumCharges.Count;
        public DateTime MomentumExpires { get; private set; }
        public DateTime RepeatedTargetExpires { get; private set; }
        public DateTime MovementAccuracyExpires { get; private set; }
        public DateTime DamageCostExpires { get; private set; }
        public int BonusAttempts { get; private set; }
        public int BonusDamagePercent { get; private set; }
        public DateTime BonusExpires { get; private set; }

        public bool HasRepeatedTargetBonus(DateTime now, PairedRangedTraitConfiguration configuration) =>
            configuration.RepeatedTargetRequiredHits > 0 &&
            _sameTargetHits >= configuration.RepeatedTargetRequiredHits && now < RepeatedTargetExpires;

        public void Reconcile(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            if (_previousExtendedDuration > configuration.MomentumExtendedDurationSeconds && Momentum > 0)
                MomentumExpires = Earlier(MomentumExpires, now.AddSeconds(configuration.MomentumDurationSeconds));
            _previousExtendedDuration = configuration.MomentumExtendedDurationSeconds;

            if (now >= MomentumExpires || configuration.MomentumMaximum <= 0)
                _momentumCharges.Clear();
            else if (Momentum > configuration.MomentumMaximum)
                _momentumCharges.RemoveRange(configuration.MomentumMaximum, Momentum - configuration.MomentumMaximum);

            if (now >= RepeatedTargetExpires || configuration.RepeatedTargetRequiredHits <= 0)
                _sameTargetHits = 0;
            if (now >= BonusExpires)
                BonusAttempts = 0;
        }

        public PairedRangedOrdinarySnapshot GetOrdinarySnapshot(
            DateTime now, PairedRangedTraitConfiguration configuration, uint target)
        {
            Reconcile(now, configuration);
            var damageBonus = configuration.OrdinaryDamagePercentAdjustment;
            if (target == _ordinaryTarget && _sameTargetHits >= configuration.RepeatedTargetRequiredHits &&
                configuration.RepeatedTargetRequiredHits > 0 && now < RepeatedTargetExpires)
                damageBonus += configuration.RepeatedTargetDamagePercent;
            if (BonusAttempts > 0)
                damageBonus += BonusDamagePercent;

            return new PairedRangedOrdinarySnapshot(
                damageBonus,
                Momentum * configuration.MomentumAccuracyPerCharge);
        }

        public PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(
            DateTime now, PairedRangedTraitConfiguration configuration, uint target, long rollId)
        {
            Reconcile(now, configuration);
            RecordTargetChange(target);

            var snapshot = GetOrdinarySnapshot(now, configuration, target);
            if (Remember(_attempts, _attemptOrder, rollId) && BonusAttempts > 0)
                BonusAttempts--;
            return snapshot;
        }

        /// <returns>Stamina to restore once for this eligible landed roll.</returns>
        public int RecordOrdinaryHit(DateTime now, PairedRangedTraitConfiguration configuration, uint target, long rollId)
        {
            Reconcile(now, configuration);
            if (!_attempts.Contains(rollId) || !Remember(_hits, _hitOrder, rollId) || target != _ordinaryTarget)
                return 0;

            if (configuration.RepeatedTargetRequiredHits > 0)
            {
                _sameTargetHits = Math.Min(_sameTargetHits + 1, configuration.RepeatedTargetRequiredHits);
                RepeatedTargetExpires = now.AddSeconds(configuration.RepeatedTargetDurationSeconds);
            }

            if (configuration.OrdinaryHitRestoreCount <= 0 || now < _restoreReady)
            {
                _ordinaryHitCount = 0;
                return 0;
            }

            if (++_ordinaryHitCount < configuration.OrdinaryHitRestoreCount)
                return 0;

            _ordinaryHitCount = 0;
            _restoreReady = now.AddSeconds(configuration.OrdinaryHitRestoreCooldownSeconds);
            return Math.Max(0, configuration.OrdinaryHitStaminaRestore);
        }

        public void RecordDirectHostileDamage(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            Reconcile(now, configuration);
            if (configuration.DirectDamageNextAbilityStaminaReduction <= 0 || now < _damageCostGrantReady)
                return;
            DamageCostExpires = now.AddSeconds(configuration.DirectDamageCostWindowSeconds);
            _damageCostGrantReady = now.AddSeconds(configuration.DirectDamageCostCooldownSeconds);
        }

        public int GetAbilityStaminaCostAdjustment(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            return now < DamageCostExpires ? -Math.Max(0, configuration.DirectDamageNextAbilityStaminaReduction) : 0;
        }

        public int ConsumeAbilityStaminaDiscount(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            var adjustment = GetAbilityStaminaCostAdjustment(now, configuration);
            DamageCostExpires = default;
            return adjustment;
        }

        public void RecordAccuracyEvasionMiss(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            Reconcile(now, configuration);
            if (configuration.MomentumMaximum <= 0 || configuration.EvasionMomentumCooldownSeconds <= 0 || now < _evasionGrantReady)
                return;
            GainMomentum(now, configuration);
            _evasionGrantReady = now.AddSeconds(configuration.EvasionMomentumCooldownSeconds);
        }

        public void RecordMovementHit(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            Reconcile(now, configuration);
            GainMomentum(now, configuration);
            if (configuration.MovementNextAbilityAccuracy > 0 && now >= _movementAccuracyGrantReady)
            {
                MovementAccuracyExpires = now.AddSeconds(configuration.MovementAccuracyDurationSeconds);
                _movementAccuracyChargeId++;
                _movementAccuracyGrantReady = now.AddSeconds(configuration.MovementAccuracyCooldownSeconds);
            }
        }

        public PairedRangedAbilityTraitsSnapshot BeginAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, bool crossfire, bool spendsMomentum)
        {
            var snapshot = CaptureAbility(now, configuration, crossfire, spendsMomentum);
            CommitAbility(now, configuration, snapshot);
            return snapshot;
        }

        /// <summary>Captures existing charge identities without consuming them during a cast.</summary>
        public PairedRangedAbilityTraitsSnapshot CaptureAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, bool crossfire, bool spendsMomentum)
        {
            Reconcile(now, configuration);
            var charges = spendsMomentum ? Momentum : 0;
            var accuracy = crossfire && now < MovementAccuracyExpires ? configuration.MovementNextAbilityAccuracy : 0;
            return new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), crossfire, spendsMomentum, charges, accuracy)
            {
                MomentumChargeIds = spendsMomentum ? _momentumCharges.ToArray() : Array.Empty<long>(),
                MovementAccuracyChargeId = accuracy > 0 ? _movementAccuracyChargeId : 0,
                StateGeneration = _stateGeneration
            };
        }

        /// <summary>
        /// Commits one successfully paid activation. Expiry cannot alter its captured payload,
        /// and newer charges are never spent in place of charges that expired during the cast.
        /// </summary>
        public bool CommitAbility(DateTime now, PairedRangedTraitConfiguration configuration,
            PairedRangedAbilityTraitsSnapshot snapshot)
        {
            Reconcile(now, configuration);
            if (snapshot == null || snapshot.StateGeneration != _stateGeneration ||
                !Remember(_committedActivations, _commitOrder, snapshot.ActivationId))
                return false;
            if (snapshot.SpendsMomentum)
            {
                var captured = new HashSet<long>(snapshot.MomentumChargeIds);
                _momentumCharges.RemoveAll(captured.Contains);
            }
            if (snapshot.UsesMovementAccuracy && snapshot.MovementAccuracyChargeId != 0 &&
                snapshot.MovementAccuracyChargeId == _movementAccuracyChargeId)
                MovementAccuracyExpires = default;
            return true;
        }

        /// <returns>One activation's recovery, capped by the stamina actually paid.</returns>
        public int CompleteAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, PairedRangedAbilityTraitsSnapshot snapshot,
            bool anyHit, float distanceTravelled, bool generatesMomentum, int paidStamina, int restoreStaminaOnHit,
            int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0)
        {
            Reconcile(now, configuration);
            if (snapshot == null || snapshot.StateGeneration != _stateGeneration ||
                !_committedActivations.Contains(snapshot.ActivationId) ||
                !Remember(_completedActivations, _activationOrder, snapshot.ActivationId))
                return 0;

            if (ordinaryBonusAttempts > 0 && ordinaryBonusSeconds > 0)
            {
                BonusAttempts = ordinaryBonusAttempts;
                BonusDamagePercent = ordinaryBonusDamagePercent;
                BonusExpires = now.AddSeconds(ordinaryBonusSeconds);
            }
            if (!anyHit)
                return 0;

            if (generatesMomentum && distanceTravelled >= 2f)
                GainMomentum(now, configuration);

            // The source activation consumed its own snapshot before granting this new charge.
            if (snapshot.UsesMovementAccuracy && distanceTravelled >= 2f && configuration.MovementNextAbilityAccuracy > 0 && now >= _movementAccuracyGrantReady)
            {
                MovementAccuracyExpires = now.AddSeconds(configuration.MovementAccuracyDurationSeconds);
                _movementAccuracyChargeId++;
                _movementAccuracyGrantReady = now.AddSeconds(configuration.MovementAccuracyCooldownSeconds);
            }

            var restore = Math.Max(0, restoreStaminaOnHit);
            if (snapshot.SpendsMomentum && snapshot.MomentumSpent > 0 && configuration.MomentumStaminaRestorePerCharge > 0 && now >= _momentumRestoreReady)
            {
                restore += Math.Min(snapshot.MomentumSpent * configuration.MomentumStaminaRestorePerCharge,
                    configuration.MomentumStaminaRestoreMaximum);
                _momentumRestoreReady = now.AddSeconds(configuration.MomentumRestoreCooldownSeconds);
            }
            return Math.Min(Math.Max(0, paidStamina), restore);
        }

        public void ClearTransientState()
        {
            _momentumCharges.Clear();
            _stateGeneration++;
            _ordinaryHitCount = 0;
            _sameTargetHits = 0;
            _ordinaryTarget = 0;
            MomentumExpires = default;
            RepeatedTargetExpires = default;
            MovementAccuracyExpires = default;
            DamageCostExpires = default;
            BonusAttempts = 0;
            BonusExpires = default;
            // Proc grant/recovery cooldowns and deduplication history deliberately survive resets.
        }

        public void GrantOrdinaryBonus(DateTime now, int attempts, int damagePercent, int seconds)
        {
            BonusAttempts = attempts;
            BonusDamagePercent = damagePercent;
            BonusExpires = now.AddSeconds(seconds);
        }

        public void RecordTargetChange(uint target)
        {
            if (_ordinaryTarget == target)
                return;
            _ordinaryTarget = target;
            _sameTargetHits = 0;
            RepeatedTargetExpires = default;
        }

        public void ClampMomentumDuration(DateTime now, int durationSeconds)
        {
            if (Momentum > 0)
                MomentumExpires = Earlier(MomentumExpires, now.AddSeconds(durationSeconds));
            _previousExtendedDuration = 0;
        }

        private void GainMomentum(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            if (configuration.MomentumMaximum <= 0)
                return;
            if (Momentum < configuration.MomentumMaximum)
                _momentumCharges.Add(++_nextMomentumChargeId);
            MomentumExpires = now.AddSeconds(Math.Max(configuration.MomentumDurationSeconds, configuration.MomentumExtendedDurationSeconds));
        }

        private static DateTime Earlier(DateTime left, DateTime right) => left < right ? left : right;

        private static bool Remember<T>(HashSet<T> known, Queue<T> order, T identity)
        {
            if (!known.Add(identity))
                return false;
            order.Enqueue(identity);
            if (order.Count > 256)
                known.Remove(order.Dequeue());
            return true;
        }
    }
}
