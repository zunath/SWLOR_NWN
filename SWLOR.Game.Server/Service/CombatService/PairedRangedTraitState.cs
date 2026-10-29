using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Deterministic per-attacker state. Callers provide only eligible ordinary rolls, direct
    /// hostile damage, accuracy-versus-evasion misses and paid weapon abilities; no perk identities enter this class.
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
        private readonly HashSet<Guid> _cadenceActivations = new();
        private readonly Queue<Guid> _cadenceOrder = new();
        private readonly List<long> _cadenceCharges = new();
        private long _nextCadenceChargeId;
        private long _cadenceAccuracyChargeId;
        private int _stateGeneration;
        private int _ordinaryHitCount;
        private int _sameTargetHits;
        private uint _ordinaryTarget;
        private int _lastAbilityKey;
        private DateTime _lastAbilityUsed;
        private DateTime _restoreReady;
        private DateTime _damageCostGrantReady;
        private DateTime _cadenceAccuracyGrantReady;
        private DateTime _cadenceRestoreReady;
        private DateTime _evasionGrantReady;
        private int _previousExtendedDuration;

        public int Cadence => _cadenceCharges.Count;
        public DateTime CadenceExpires { get; private set; }
        public DateTime RepeatedTargetExpires { get; private set; }
        public DateTime CadenceAccuracyExpires { get; private set; }
        public DateTime DamageCostExpires { get; private set; }
        public int BonusAttempts { get; private set; }
        public int BonusDamagePercent { get; private set; }
        public DateTime BonusExpires { get; private set; }

        public bool HasRepeatedTargetBonus(DateTime now, PairedRangedTraitConfiguration configuration) =>
            configuration.RepeatedTargetRequiredHits > 0 &&
            _sameTargetHits >= configuration.RepeatedTargetRequiredHits && now < RepeatedTargetExpires;

        public void Reconcile(DateTime now, PairedRangedTraitConfiguration configuration)
        {
            if (_previousExtendedDuration > configuration.CadenceExtendedDurationSeconds && Cadence > 0)
                CadenceExpires = Earlier(CadenceExpires, now.AddSeconds(configuration.CadenceDurationSeconds));
            _previousExtendedDuration = configuration.CadenceExtendedDurationSeconds;

            if (now >= CadenceExpires || configuration.CadenceMaximum <= 0)
                _cadenceCharges.Clear();
            else if (Cadence > configuration.CadenceMaximum)
                _cadenceCharges.RemoveRange(configuration.CadenceMaximum, Cadence - configuration.CadenceMaximum);

            if (now >= RepeatedTargetExpires || configuration.RepeatedTargetRequiredHits <= 0)
                _sameTargetHits = 0;
            if (now >= BonusExpires)
                BonusAttempts = 0;
        }

        public PairedRangedOrdinarySnapshot GetOrdinarySnapshot(
            DateTime now, PairedRangedTraitConfiguration configuration, uint target)
        {
            Reconcile(now, configuration);
            var damageBonus = 0;
            if (target == _ordinaryTarget && _sameTargetHits >= configuration.RepeatedTargetRequiredHits &&
                configuration.RepeatedTargetRequiredHits > 0 && now < RepeatedTargetExpires)
                damageBonus += configuration.RepeatedTargetDamagePercent;
            if (BonusAttempts > 0)
                damageBonus += BonusDamagePercent;

            return new PairedRangedOrdinarySnapshot(
                damageBonus,
                Cadence * configuration.CadenceAccuracyPerCharge);
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
            if (!_attempts.Contains(rollId) || !Remember(_hits, _hitOrder, rollId))
                return 0;

            // Only the same-target streak depends on the current target; the restore counter counts every landed hit.
            if (configuration.RepeatedTargetRequiredHits > 0 && target == _ordinaryTarget)
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
            if (configuration.CadenceMaximum <= 0 || configuration.EvasionCadenceCooldownSeconds <= 0 || now < _evasionGrantReady)
                return;
            GainCadence(now, configuration, 1);
            _evasionGrantReady = now.AddSeconds(configuration.EvasionCadenceCooldownSeconds);
        }

        /// <summary>
        /// Records one committed weapon ability. Returns whether it continues a chain: a different ability than the
        /// previous one, used within the chain window. <paramref name="abilityKey"/> identifies the ability across ranks.
        /// </summary>
        public bool RecordAbilityUse(DateTime now, PairedRangedTraitConfiguration configuration, int abilityKey) =>
            RecordAbilityUse(now, configuration, abilityKey, out _);

        /// <param name="landsBy">When chained, the latest moment its hit may land and still continue the chain.</param>
        public bool RecordAbilityUse(DateTime now, PairedRangedTraitConfiguration configuration, int abilityKey,
            out DateTime landsBy)
        {
            landsBy = configuration.CadenceChainWindowSeconds > 0
                ? _lastAbilityUsed.AddSeconds(configuration.CadenceChainWindowSeconds)
                : DateTime.MinValue;
            var chained = configuration.CadenceChainWindowSeconds > 0 && _lastAbilityKey != 0 &&
                          _lastAbilityKey != abilityKey && now <= landsBy;
            _lastAbilityKey = abilityKey;
            _lastAbilityUsed = now;
            return chained;
        }

        /// <summary>Grants an activation's Cadence once, however many hands, targets, repeats or waves land.</summary>
        public void RecordAbilityHit(DateTime now, PairedRangedTraitConfiguration configuration, Guid activationId, int charges)
        {
            Reconcile(now, configuration);
            if (charges <= 0 || configuration.CadenceMaximum <= 0 ||
                !Remember(_cadenceActivations, _cadenceOrder, activationId))
                return;
            GainCadence(now, configuration, charges);
        }

        public PairedRangedAbilityTraitsSnapshot BeginAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            var snapshot = CaptureAbility(now, configuration, usesNextAbilityAccuracy, spendsCadence);
            CommitAbility(now, configuration, snapshot);
            return snapshot;
        }

        /// <summary>Captures existing charge identities without consuming them during a cast.</summary>
        public PairedRangedAbilityTraitsSnapshot CaptureAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            Reconcile(now, configuration);
            var charges = spendsCadence ? Cadence : 0;
            var accuracy = usesNextAbilityAccuracy && now < CadenceAccuracyExpires ? configuration.CadenceNextAbilityAccuracy : 0;
            return new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), usesNextAbilityAccuracy, spendsCadence, charges, accuracy)
            {
                CadenceChargeIds = spendsCadence ? _cadenceCharges.ToArray() : Array.Empty<long>(),
                CadenceAccuracyChargeId = accuracy > 0 ? _cadenceAccuracyChargeId : 0,
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
            if (snapshot.SpendsCadence)
            {
                var captured = new HashSet<long>(snapshot.CadenceChargeIds);
                _cadenceCharges.RemoveAll(captured.Contains);
            }
            if (snapshot.UsesNextAbilityAccuracy && snapshot.CadenceAccuracyChargeId != 0 &&
                snapshot.CadenceAccuracyChargeId == _cadenceAccuracyChargeId)
                CadenceAccuracyExpires = default;
            return true;
        }

        /// <returns>One activation's recovery, capped by the stamina actually paid.</returns>
        public int CompleteAbility(
            DateTime now, PairedRangedTraitConfiguration configuration, PairedRangedAbilityTraitsSnapshot snapshot,
            bool anyHit, int paidStamina, int restoreStaminaOnHit,
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

            var restore = Math.Max(0, restoreStaminaOnHit);
            if (snapshot.SpendsCadence && snapshot.CadenceSpent > 0 && configuration.CadenceStaminaRestorePerCharge > 0 && now >= _cadenceRestoreReady)
            {
                restore += Math.Min(snapshot.CadenceSpent * configuration.CadenceStaminaRestorePerCharge,
                    configuration.CadenceStaminaRestoreMaximum);
                _cadenceRestoreReady = now.AddSeconds(configuration.CadenceRestoreCooldownSeconds);
            }
            return Math.Min(Math.Max(0, paidStamina), restore);
        }

        public void ClearTransientState()
        {
            _cadenceCharges.Clear();
            _stateGeneration++;
            _ordinaryHitCount = 0;
            _sameTargetHits = 0;
            _ordinaryTarget = 0;
            _lastAbilityKey = 0;
            _lastAbilityUsed = default;
            CadenceExpires = default;
            RepeatedTargetExpires = default;
            CadenceAccuracyExpires = default;
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

        public void ClampCadenceDuration(DateTime now, int durationSeconds)
        {
            if (Cadence > 0)
                CadenceExpires = Earlier(CadenceExpires, now.AddSeconds(durationSeconds));
            _previousExtendedDuration = 0;
        }

        /// <summary>Adds charges up to the cap and refreshes their duration; any gain also offers the next-ability accuracy.</summary>
        private void GainCadence(DateTime now, PairedRangedTraitConfiguration configuration, int charges)
        {
            if (configuration.CadenceMaximum <= 0)
                return;
            for (var i = 0; i < charges && Cadence < configuration.CadenceMaximum; i++)
                _cadenceCharges.Add(++_nextCadenceChargeId);
            CadenceExpires = now.AddSeconds(Math.Max(configuration.CadenceDurationSeconds, configuration.CadenceExtendedDurationSeconds));
            if (configuration.CadenceNextAbilityAccuracy > 0 && now >= _cadenceAccuracyGrantReady)
            {
                CadenceAccuracyExpires = now.AddSeconds(configuration.CadenceAccuracyDurationSeconds);
                _cadenceAccuracyChargeId++;
                _cadenceAccuracyGrantReady = now.AddSeconds(configuration.CadenceAccuracyCooldownSeconds);
            }
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
