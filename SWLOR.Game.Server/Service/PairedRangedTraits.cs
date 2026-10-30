using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Engine adapter for stat-configured weapon traits and captured ability charges.</summary>
    public static class PairedRangedTraits
    {
        private static readonly Dictionary<uint, PairedRangedTraitState> States = new();

        /// <summary>Reports a weapon ability's first landed hit; its chain or own grant adds Cadence once.</summary>
        public static void RecordWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation)
        {
            var now = DateTime.UtcNow;
            if (activation?.IsCommitted != true || activation.Snapshot.StateGeneration == -1 ||
                activation.CadenceEarnedOnHitAt(now) <= 0 || !CanReceiveTraitEvents(actor))
                return;
            var state = GetState(actor);
            state.RecordAbilityHit(now, GetConfiguration(actor), activation.Snapshot.ActivationId,
                activation.CadenceEarnedOnHitAt(now));
            Sync(actor, state);
        }

        public static WeaponAbilityTraitActivation CaptureWeaponAbility(uint actor, AbilityDetail ability)
        {
            if (!WeaponAbilityTraitPolicy.IsHostileWeaponAbility(ability) || !CanReceiveTraitEvents(actor)) return null;
            if (Read(actor, StatType.CadenceMaximum) <= 0 &&
                Read(actor, StatType.CadenceNextAbilityAccuracy) <= 0)
                return new WeaponAbilityTraitActivation(
                    new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0) { StateGeneration = -1 }, 0);
            var configuration = GetConfiguration(actor);
            var damagePerCharge = (ability.WeaponAbilityCadenceDamagePerCharge ?? configuration.CadenceDamagePerCharge) +
                                  configuration.CadenceBonusDamagePerCharge;
            // Without the Cadence trait a foreign ability gains nothing per charge, so it leaves the charges alone.
            var spendsCadence = !ability.KeepsWeaponAbilityCadence && damagePerCharge > 0;
            return new WeaponAbilityTraitActivation(CaptureAbility(actor, true, spendsCadence), damagePerCharge)
                {
                    PrimaryWeapon = Combat.GetAbilityAttackWeapon(actor, ability.SkillType),
                    CadenceOnHit = ability.WeaponAbilityCadenceOnHit
                };
        }

        public static void CommitWeaponAbility(uint actor, AbilityDetail ability, WeaponAbilityTraitActivation activation)
        {
            if (activation == null || activation.IsCommitted) return;
            activation.SetPaidStamina(Combat.GetAbilityStaminaPaid(actor, ability));
            if (activation.Snapshot.StateGeneration == -1)
            {
                activation.IsCommitted = true;
                return;
            }
            activation.IsCommitted = CommitAbility(actor, activation.Snapshot);
            if (activation.IsCommitted && States.TryGetValue(actor, out var state))
            {
                activation.ContinuesChain = state.RecordAbilityUse(DateTime.UtcNow, GetConfiguration(actor), ChainKey(ability),
                    out var landsBy);
                activation.ChainLandsBy = landsBy;
            }
        }

        /// <summary>Every rank of one ability shares a key, so a rank change is not a different ability.</summary>
        private static int ChainKey(AbilityDetail ability) =>
            ability.EffectiveLevelPerkType != PerkType.Invalid
                ? (int)ability.EffectiveLevelPerkType
                : StringComparer.Ordinal.GetHashCode(ability.Name ?? string.Empty) | int.MinValue;

        public static void CompleteWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation)
        {
            if (activation?.IsCommitted != true || activation.RecoveryCompleted || activation.Snapshot.StateGeneration == -1) return;
            activation.RecoveryCompleted = true;
            CompleteAbility(actor, activation.Snapshot, true, activation.PaidStamina, 0);
        }

        public static void CompleteIntrinsicAbility(uint actor, WeaponAbilityTraitActivation activation, bool anyHit,
            int paidStamina, int restoreStamina, int bonusAttempts, int bonusDamagePercent, int bonusSeconds)
        {
            if (activation == null || activation.IntrinsicCompleted) return;
            activation.IntrinsicCompleted = true;
            if (bonusAttempts > 0 && bonusSeconds > 0)
            {
                var state = GetState(actor);
                state.GrantOrdinaryBonus(DateTime.UtcNow, bonusAttempts, bonusDamagePercent, bonusSeconds);
                Sync(actor, state);
            }
            if (anyHit && restoreStamina > 0)
                Combat.RestoreAbilityHitStamina(actor, Ability.GetActiveAbilityImpactSummary(actor)?.Ability,
                    Math.Min(Math.Max(0, paidStamina), restoreStamina));
        }

        public static PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(uint actor, uint target, long rollId)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            TwinPistolMastery.RecordTargetChange(actor, target);
            if (!States.TryGetValue(actor, out var state))
            {
                // Untracked attackers need only the ordinary-shot inputs, which keeps every creature's roll cheap.
                var ordinary = GetOrdinaryConfiguration(actor);
                if (!NeedsOrdinaryState(ordinary))
                    return UntrackedSnapshot(ordinary);
                state = GetState(actor);
            }
            var snapshot = state.RecordOrdinaryAttempt(DateTime.UtcNow, GetConfiguration(actor), target, rollId);
            Sync(actor, state);
            return snapshot;
        }

        public static void RecordOrdinaryHit(uint actor, uint target, long rollId)
        {
            if (!CanReceiveTraitEvents(actor) || !GetIsObjectValid(target) || target == actor ||
                !GetIsReactionTypeHostile(target, actor))
                return;
            if (!States.TryGetValue(actor, out var state)) return;
            var stamina = state.RecordOrdinaryHit(DateTime.UtcNow, GetConfiguration(actor), target, rollId);
            if (stamina > 0)
                Stat.RestoreStamina(actor, stamina);
            Sync(actor, state);
        }

        public static int GetOrdinaryDamagePercentAdjustment(uint actor, uint target) =>
            GetSnapshot(actor, target).DamagePercentAdjustment;

        public static int GetOrdinaryAccuracy(uint actor) =>
            GetSnapshot(actor, 0).AccuracyAdjustment;

        public static void RecordTargetChange(uint actor, uint target)
        {
            TwinPistolMastery.RecordTargetChange(actor, target);
            if (!States.TryGetValue(actor, out var state))
                return;
            state.RecordTargetChange(target);
            Sync(actor, state);
        }

        /// <summary>Lets callers skip mitigation work for defenders that have no direct-damage trait.</summary>
        public static bool HasDirectHostileDamageTrait(uint actor) =>
            CanReceiveTraitEvents(actor) && Read(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction) > 0;

        /// <summary>Only positive direct hostile damage may call this hook.</summary>
        public static void RecordDirectHostileDamage(uint actor)
        {
            if (!HasDirectHostileDamageTrait(actor))
                return;
            var configuration = GetConfiguration(actor);
            if (configuration.DirectDamageNextAbilityStaminaReduction <= 0) return;
            var state = GetState(actor);
            state.RecordDirectHostileDamage(DateTime.UtcNow, configuration);
            Sync(actor, state);
        }

        /// <summary>Only a direct hostile accuracy-versus-evasion miss may call this hook.</summary>
        public static void RecordAccuracyEvasionMiss(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
                return;
            var configuration = GetConfiguration(actor);
            if (configuration.CadenceMaximum <= 0 || configuration.EvasionCadenceCooldownSeconds <= 0) return;
            var state = GetState(actor);
            state.RecordAccuracyEvasionMiss(DateTime.UtcNow, configuration);
            Sync(actor, state);
        }

        public static int GetAbilityStaminaCostAdjustment(uint actor)
        {
            if (!CanReceiveTraitEvents(actor) || !States.TryGetValue(actor, out var state))
                return 0;
            return state.GetAbilityStaminaCostAdjustment(DateTime.UtcNow, GetConfiguration(actor));
        }

        public static int ConsumeAbilityStaminaDiscount(uint actor)
        {
            if (!CanReceiveTraitEvents(actor) || !States.TryGetValue(actor, out var state))
                return 0;
            var adjustment = state.ConsumeAbilityStaminaDiscount(DateTime.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
            return adjustment;
        }

        public static PairedRangedAbilityTraitsSnapshot BeginAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            var snapshot = CaptureAbility(actor, usesNextAbilityAccuracy, spendsCadence);
            CommitAbility(actor, snapshot);
            return snapshot;
        }

        public static PairedRangedAbilityTraitsSnapshot CaptureAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), usesNextAbilityAccuracy, spendsCadence, 0, 0)
                    { StateGeneration = -1 };
            var state = GetState(actor);
            var snapshot = state.CaptureAbility(DateTime.UtcNow, GetConfiguration(actor), usesNextAbilityAccuracy, spendsCadence);
            Sync(actor, state);
            return snapshot;
        }

        public static bool CommitAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot)
        {
            if (!CanReceiveTraitEvents(actor) || !States.TryGetValue(actor, out var state))
                return false;
            var committed = state.CommitAbility(DateTime.UtcNow, GetConfiguration(actor), snapshot);
            Sync(actor, state);
            return committed;
        }

        public static void CompleteAbility(
            uint actor, PairedRangedAbilityTraitsSnapshot snapshot, bool anyHit, int paidStamina, int restoreStaminaOnHit,
            int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0)
        {
            if (!CanReceiveTraitEvents(actor))
                return;
            var state = GetState(actor);
            var restore = state.CompleteAbility(
                DateTime.UtcNow, GetConfiguration(actor), snapshot, anyHit, paidStamina, restoreStaminaOnHit, ordinaryBonusAttempts, ordinaryBonusDamagePercent, ordinaryBonusSeconds);
            if (restore > 0)
                Combat.RestoreAbilityHitStamina(actor, Ability.GetActiveAbilityImpactSummary(actor)?.Ability, restore);
            Sync(actor, state);
        }

        public static void Reconcile(uint actor)
        {
            if (!States.TryGetValue(actor, out var state))
                return;
            state.Reconcile(DateTime.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
        }

        public static void ExitExtendedCadence(uint actor)
        {
            if (!States.TryGetValue(actor, out var state))
                return;
            var duration = Read(actor, StatType.CadenceDurationSeconds);
            state.ClampCadenceDuration(DateTime.UtcNow, duration);
            Sync(actor, state);
        }

        public static void ClearCombatState(uint actor) => ClearTransientState(actor);

        public static void ClearEquipmentState(uint actor)
        {
            Reconcile(actor);
        }

        public static void ClearActor(uint actor)
        {
            ClearTransientState(actor);
            States.Remove(actor);
        }

        /// <summary>Drops tracking for a destroyed object without refreshing its status effects.</summary>
        public static void ForgetDestroyed(uint actor)
        {
            States.Remove(actor);
            TwinPistolMastery.ClearTrialState(actor);
        }

        private static void ClearTransientState(uint actor)
        {
            TwinPistolMastery.ClearTrialState(actor);
            if (!States.TryGetValue(actor, out var state))
                return;
            state.ClearTransientState();
            Sync(actor, state);
        }

        private static PairedRangedOrdinarySnapshot GetSnapshot(uint actor, uint target)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            return States.TryGetValue(actor, out var state)
                ? state.GetOrdinarySnapshot(DateTime.UtcNow, GetConfiguration(actor), target)
                : new PairedRangedOrdinarySnapshot(0, 0);
        }

        private static bool NeedsOrdinaryState(PairedRangedTraitConfiguration configuration) =>
            configuration.OrdinaryHitRestoreCount > 0 && configuration.OrdinaryHitStaminaRestore > 0 ||
            configuration.RepeatedTargetRequiredHits > 0 && configuration.RepeatedTargetDamagePercent != 0;

        private static PairedRangedOrdinarySnapshot UntrackedSnapshot(PairedRangedTraitConfiguration configuration) =>
            new(0, 0);

        /// <summary>The subset of <see cref="GetConfiguration"/> read by <see cref="NeedsOrdinaryState"/> and <see cref="UntrackedSnapshot"/>.</summary>
        private static PairedRangedTraitConfiguration GetOrdinaryConfiguration(uint actor) => new()
        {
            OrdinaryHitRestoreCount = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
            OrdinaryHitStaminaRestore = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
            RepeatedTargetRequiredHits = Read(actor, StatType.PairedRangedRepeatedTargetRequiredHits),
            RepeatedTargetDamagePercent = Read(actor, StatType.PairedRangedRepeatedTargetDamagePercent),
        };

        private static PairedRangedTraitState GetState(uint actor)
        {
            if (!States.TryGetValue(actor, out var state))
                States[actor] = state = new PairedRangedTraitState();
            return state;
        }

        private static int Read(uint actor, StatType stat) => Stat.GetStatAdjustment(actor, stat);

        // Paired ordinary-damage and Alternating Fire offset the paired firing cycle, so they need two weapons.
        private static int ReadDualWield(uint actor, StatType stat) =>
            EquipmentPredicates.HasDualWield(actor) ? Read(actor, stat) : 0;

        private static bool CanReceiveTraitEvents(uint actor) =>
            GetIsObjectValid(actor) && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0;

        public static PairedRangedTraitConfiguration GetConfiguration(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedTraitConfiguration();
            return new PairedRangedTraitConfiguration
            {
                OrdinaryHitRestoreCount = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
                OrdinaryHitStaminaRestore = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
                OrdinaryHitRestoreCooldownSeconds = Read(actor, StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds),
                RepeatedTargetRequiredHits = Read(actor, StatType.PairedRangedRepeatedTargetRequiredHits),
                RepeatedTargetDamagePercent = Read(actor, StatType.PairedRangedRepeatedTargetDamagePercent),
                RepeatedTargetDurationSeconds = Read(actor, StatType.PairedRangedRepeatedTargetDurationSeconds),
                DirectDamageNextAbilityStaminaReduction = Read(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction),
                DirectDamageCostWindowSeconds = Read(actor, StatType.PairedRangedDirectDamageCostWindowSeconds),
                DirectDamageCostCooldownSeconds = Read(actor, StatType.PairedRangedDirectDamageCostCooldownSeconds),
                CadenceMaximum = Read(actor, StatType.CadenceMaximum),
                CadenceDurationSeconds = Read(actor, StatType.CadenceDurationSeconds),
                CadenceChainWindowSeconds = Read(actor, StatType.CadenceChainWindowSeconds),
                CadenceAccuracyPerCharge = Read(actor, StatType.CadenceAccuracyPerCharge),
                CadenceDamagePerCharge = Read(actor, StatType.CadenceDamagePerCharge),
                CadenceBonusDamagePerCharge = Read(actor, StatType.CadenceBonusDamagePerCharge),
                CadenceStaminaRestorePerCharge = Read(actor, StatType.CadenceStaminaRestorePerCharge),
                CadenceStaminaRestoreMaximum = Read(actor, StatType.CadenceStaminaRestoreMaximum),
                CadenceRestoreCooldownSeconds = Read(actor, StatType.CadenceRestoreCooldownSeconds),
                EvasionCadenceCooldownSeconds = Read(actor, StatType.EvasionCadenceCooldownSeconds),
                CadenceNextAbilityAccuracy = Read(actor, StatType.CadenceNextAbilityAccuracy),
                CadenceAccuracyDurationSeconds = Read(actor, StatType.CadenceAccuracyDurationSeconds),
                CadenceAccuracyCooldownSeconds = Read(actor, StatType.CadenceAccuracyCooldownSeconds),
                CadenceExtendedDurationSeconds = Read(actor, StatType.CadenceExtendedDurationSeconds),
            };
        }

        private static void Sync(uint actor, PairedRangedTraitState state)
        {
            if (!GetIsObjectValid(actor))
                return;
            var now = DateTime.UtcNow;
            var configuration = GetConfiguration(actor);
            Refresh(actor, new CadenceStatusEffect(state.Cadence), Remaining(state.CadenceExpires, now), state.Cadence > 0);
            Refresh(actor, new ConvergingFireStatusEffect(), Remaining(state.RepeatedTargetExpires, now),
                state.HasRepeatedTargetBonus(now, configuration));
            Refresh(actor, new ChangingAnglesStatusEffect(), Remaining(state.CadenceAccuracyExpires, now),
                state.CadenceAccuracyExpires > now);
            Refresh(actor, new CoolUnderFireStatusEffect(), Remaining(state.DamageCostExpires, now),
                state.DamageCostExpires > now);
            Refresh(actor, new TwinSunsStatusEffect(state.BonusAttempts), Remaining(state.BonusExpires, now),
                state.BonusAttempts > 0);
        }

        private static float Remaining(DateTime expires, DateTime now) => (float)Math.Max(0d, (expires - now).TotalSeconds);

        private static void Refresh<T>(uint actor, T desired, float seconds, bool active) where T : StatusEffectBase
        {
            var existing = StatusEffect.GetStatusEffect<T>(actor);
            if (!active || seconds <= 0f)
            {
                if (existing != null)
                    StatusEffect.RemoveStatusEffect<T>(actor);
                return;
            }
            if (existing != null && existing.Name == desired.Name && existing.Icon == desired.Icon)
            {
                if (Math.Abs(existing.GetRemainingDurationSeconds(DateTime.UtcNow) - seconds) > .01f)
                {
                    existing.SetDurationSeconds(seconds);
                    StatusEffect.RefreshStatusEffectDuration(actor, typeof(T), actor, seconds);
                }
                return;
            }
            StatusEffect.RemoveStatusEffect<T>(actor);
            StatusEffect.ApplyStatusEffect(actor, actor, desired, seconds);
        }
    }
}

