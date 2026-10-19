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
        private static readonly Dictionary<uint, WeaponMovementWindow> MovementWindows = new();

        public static void TrackMovement(uint actor)
        {
            if (!CanReceiveTraitEvents(actor) || MovementWindows.ContainsKey(actor) ||
                Read(actor, StatType.PairedRangedMomentumMaximum) <= 0 &&
                Read(actor, StatType.PairedRangedMovementNextAbilityAccuracy) <= 0) return;
            var window = new WeaponMovementWindow();
            MovementWindows[actor] = window;
            ObserveMovement(actor, window);
            void Sample()
            {
                if (!CanReceiveTraitEvents(actor) || !MovementWindows.TryGetValue(actor, out var current) || current != window)
                    return;
                ObserveMovement(actor, window);
                AssignCommand(GetModule(), () => DelayCommand(.25f, Sample));
            }
            AssignCommand(GetModule(), () => DelayCommand(.25f, Sample));
        }

        private static void ObserveMovement(uint actor, WeaponMovementWindow window) =>
            window.Observe(DateTime.UtcNow, GetArea(actor), GetPosition(actor));

        public static void RecordLandedWeaponHit(uint actor, uint target)
        {
            if (!CanReceiveTraitEvents(actor) || !GetIsObjectValid(target) || target == actor ||
                !GetIsReactionTypeHostile(target, actor)) return;
            TrackMovement(actor);
            if (!MovementWindows.TryGetValue(actor, out var window)) return;
            ObserveMovement(actor, window);
            if (!window.TryConsumeForHit(GetArea(actor), GetPosition(actor))) return;
            var state = GetState(actor);
            state.RecordMovementHit(DateTime.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
        }

        public static WeaponAbilityTraitActivation CaptureWeaponAbility(uint actor, AbilityDetail ability)
        {
            TrackMovement(actor);
            if (!WeaponAbilityTraitPolicy.IsHostileWeaponAbility(ability) || !CanReceiveTraitEvents(actor)) return null;
            if (Read(actor, StatType.PairedRangedMomentumMaximum) <= 0 &&
                Read(actor, StatType.PairedRangedMovementNextAbilityAccuracy) <= 0)
                return new WeaponAbilityTraitActivation(
                    new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0) { StateGeneration = -1 }, 0);
            var configuration = GetConfiguration(actor);
            return new WeaponAbilityTraitActivation(CaptureAbility(actor, true, true),
                ability.WeaponAbilityFlowDamagePerCharge ?? configuration.MomentumDamagePerCharge)
                { PrimaryWeapon = Combat.GetAbilityAttackWeapon(actor, ability.SkillType) };
        }

        public static void CommitWeaponAbility(uint actor, AbilityDetail ability, WeaponAbilityTraitActivation activation)
        {
            if (activation == null || activation.IsCommitted) return;
            activation.SetPaidStamina(Combat.GetAbilityStaminaPaid(actor, ability));
            activation.IsCommitted = activation.Snapshot.StateGeneration == -1 || CommitAbility(actor, activation.Snapshot);
        }

        public static void CompleteWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation)
        {
            if (activation?.IsCommitted != true || activation.RecoveryCompleted || activation.Snapshot.StateGeneration == -1) return;
            activation.RecoveryCompleted = true;
            CompleteAbility(actor, activation.Snapshot, true, 0f, false, activation.PaidStamina, 0);
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

        public static PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(uint actor, uint target, long rollId, bool companion = false)
        {
            TrackMovement(actor);
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            TwinPistolMastery.RecordTargetChange(actor, target);
            var configuration = GetConfiguration(actor);
            if (!States.ContainsKey(actor) && !NeedsOrdinaryState(configuration))
                return UntrackedSnapshot(configuration, companion);
            var state = GetState(actor);
            var snapshot = state.RecordOrdinaryAttempt(DateTime.UtcNow, configuration, target, rollId, companion);
            Sync(actor, state);
            return snapshot;
        }

        public static void RecordOrdinaryHit(uint actor, uint target, long rollId)
        {
            if (!CanReceiveTraitEvents(actor) || !GetIsObjectValid(target) || target == actor ||
                !GetIsReactionTypeHostile(target, actor))
                return;
            TrackMovement(actor);
            if (MovementWindows.TryGetValue(actor, out var movement) && !movement.TryRememberOrdinaryHit(rollId)) return;
            RecordLandedWeaponHit(actor, target);
            if (!States.TryGetValue(actor, out var state)) return;
            var stamina = state.RecordOrdinaryHit(DateTime.UtcNow, GetConfiguration(actor), target, rollId);
            if (stamina > 0)
                Stat.RestoreStamina(actor, stamina);
            Sync(actor, state);
        }

        public static int GetOrdinaryDamagePercentAdjustment(uint actor, uint target) =>
            GetSnapshot(actor, target, false).DamagePercentAdjustment;

        public static int GetOrdinaryAccuracy(uint actor, bool companion) =>
            GetSnapshot(actor, 0, companion).AccuracyAdjustment;

        public static void RecordTargetChange(uint actor, uint target)
        {
            TwinPistolMastery.RecordTargetChange(actor, target);
            if (!States.TryGetValue(actor, out var state))
                return;
            state.RecordTargetChange(target);
            Sync(actor, state);
        }

        /// <summary>Only positive direct hostile damage may call this hook.</summary>
        public static void RecordDirectHostileDamage(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
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
            if (configuration.MomentumMaximum <= 0 || configuration.EvasionMomentumCooldownSeconds <= 0) return;
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

        public static PairedRangedAbilityTraitsSnapshot BeginAbility(uint actor, bool crossfire, bool spendsMomentum)
        {
            var snapshot = CaptureAbility(actor, crossfire, spendsMomentum);
            CommitAbility(actor, snapshot);
            return snapshot;
        }

        public static PairedRangedAbilityTraitsSnapshot CaptureAbility(uint actor, bool crossfire, bool spendsMomentum)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), crossfire, spendsMomentum, 0, 0)
                    { StateGeneration = -1 };
            var state = GetState(actor);
            var snapshot = state.CaptureAbility(DateTime.UtcNow, GetConfiguration(actor), crossfire, spendsMomentum);
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
            uint actor, PairedRangedAbilityTraitsSnapshot snapshot, bool anyHit, float distanceTravelled,
            bool generatesMomentum, int paidStamina, int restoreStaminaOnHit,
            int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0)
        {
            if (!CanReceiveTraitEvents(actor))
                return;
            var state = GetState(actor);
            var restore = state.CompleteAbility(
                DateTime.UtcNow, GetConfiguration(actor), snapshot, anyHit, distanceTravelled, generatesMomentum,
                paidStamina, restoreStaminaOnHit, ordinaryBonusAttempts, ordinaryBonusDamagePercent, ordinaryBonusSeconds);
            if (restore > 0)
                Combat.RestoreAbilityHitStamina(actor, Ability.GetActiveAbilityImpactSummary(actor)?.Ability, restore);
            Sync(actor, state);
        }

        public static void Reconcile(uint actor)
        {
            ReconcileMovementTracking(actor);
            if (!States.TryGetValue(actor, out var state))
                return;
            state.Reconcile(DateTime.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
        }

        public static void ReconcileMovementTracking(uint actor)
        {
            if (MovementWindows.TryGetValue(actor, out var movement) &&
                Read(actor, StatType.PairedRangedMomentumMaximum) <= 0 &&
                Read(actor, StatType.PairedRangedMovementNextAbilityAccuracy) <= 0)
            {
                MovementWindows.Remove(actor);
                movement.Clear();
            }
            TrackMovement(actor);
        }

        public static void ExitExtendedMomentum(uint actor)
        {
            if (!States.TryGetValue(actor, out var state))
                return;
            var duration = Read(actor, StatType.PairedRangedMomentumDurationSeconds);
            state.ClampMomentumDuration(DateTime.UtcNow, duration);
            Sync(actor, state);
        }

        public static void ClearCombatState(uint actor, bool preserveMovement = false) => ClearTransientState(actor, preserveMovement);

        public static void ClearEquipmentState(uint actor)
        {
            Reconcile(actor);
        }

        public static void ClearActor(uint actor)
        {
            ClearTransientState(actor);
            States.Remove(actor);
        }

        private static void ClearTransientState(uint actor, bool preserveMovement = false)
        {
            if (!preserveMovement && MovementWindows.Remove(actor, out var movement)) movement.Clear();
            TwinPistolMastery.ClearTrialState(actor);
            if (!States.TryGetValue(actor, out var state))
                return;
            state.ClearTransientState();
            Sync(actor, state);
        }

        private static PairedRangedOrdinarySnapshot GetSnapshot(uint actor, uint target, bool companion)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            var configuration = GetConfiguration(actor);
            return States.TryGetValue(actor, out var state)
                ? state.GetOrdinarySnapshot(DateTime.UtcNow, configuration, target, companion)
                : UntrackedSnapshot(configuration, companion);
        }

        private static bool NeedsOrdinaryState(PairedRangedTraitConfiguration configuration) =>
            configuration.OrdinaryHitRestoreCount > 0 && configuration.OrdinaryHitStaminaRestore > 0 ||
            configuration.RepeatedTargetRequiredHits > 0 && configuration.RepeatedTargetDamagePercent != 0;

        private static PairedRangedOrdinarySnapshot UntrackedSnapshot(PairedRangedTraitConfiguration configuration, bool offhand) =>
            new(configuration.OrdinaryDamagePercentAdjustment, offhand ? configuration.CompanionAccuracy : 0);

        private static PairedRangedTraitState GetState(uint actor)
        {
            if (!States.TryGetValue(actor, out var state))
                States[actor] = state = new PairedRangedTraitState();
            return state;
        }

        private static int Read(uint actor, StatType stat) => Stat.GetStatAdjustment(actor, stat);

        private static bool CanReceiveTraitEvents(uint actor) =>
            GetIsObjectValid(actor) && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0;

        public static PairedRangedTraitConfiguration GetConfiguration(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedTraitConfiguration();
            return new PairedRangedTraitConfiguration
            {
                OrdinaryDamagePercentAdjustment = Read(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment),
                CompanionAccuracy = Read(actor, StatType.PairedRangedCompanionAccuracy),
                OrdinaryHitRestoreCount = Read(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
                OrdinaryHitStaminaRestore = Read(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
                OrdinaryHitRestoreCooldownSeconds = Read(actor, StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds),
                RepeatedTargetRequiredHits = Read(actor, StatType.PairedRangedRepeatedTargetRequiredHits),
                RepeatedTargetDamagePercent = Read(actor, StatType.PairedRangedRepeatedTargetDamagePercent),
                RepeatedTargetDurationSeconds = Read(actor, StatType.PairedRangedRepeatedTargetDurationSeconds),
                DirectDamageNextAbilityStaminaReduction = Read(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction),
                DirectDamageCostWindowSeconds = Read(actor, StatType.PairedRangedDirectDamageCostWindowSeconds),
                DirectDamageCostCooldownSeconds = Read(actor, StatType.PairedRangedDirectDamageCostCooldownSeconds),
                MomentumMaximum = Read(actor, StatType.PairedRangedMomentumMaximum),
                MomentumDurationSeconds = Read(actor, StatType.PairedRangedMomentumDurationSeconds),
                MomentumAccuracyPerCharge = Read(actor, StatType.PairedRangedMomentumAccuracyPerCharge),
                MomentumDamagePerCharge = Read(actor, StatType.MovementHitWeaponAbilityDamagePerCharge),
                MomentumStaminaRestorePerCharge = Read(actor, StatType.PairedRangedMomentumStaminaRestorePerCharge),
                MomentumStaminaRestoreMaximum = Read(actor, StatType.PairedRangedMomentumStaminaRestoreMaximum),
                MomentumRestoreCooldownSeconds = Read(actor, StatType.PairedRangedMomentumRestoreCooldownSeconds),
                EvasionMomentumCooldownSeconds = Read(actor, StatType.PairedRangedEvasionMomentumCooldownSeconds),
                MovementNextAbilityAccuracy = Read(actor, StatType.PairedRangedMovementNextAbilityAccuracy),
                MovementAccuracyDurationSeconds = Read(actor, StatType.PairedRangedMovementAccuracyDurationSeconds),
                MovementAccuracyCooldownSeconds = Read(actor, StatType.PairedRangedMovementAccuracyCooldownSeconds),
                MomentumExtendedDurationSeconds = Read(actor, StatType.PairedRangedMomentumExtendedDurationSeconds),
            };
        }

        private static void Sync(uint actor, PairedRangedTraitState state)
        {
            if (!GetIsObjectValid(actor))
                return;
            var now = DateTime.UtcNow;
            var configuration = GetConfiguration(actor);
            Refresh(actor, new MomentumStatusEffect(state.Momentum), Remaining(state.MomentumExpires, now), state.Momentum > 0);
            Refresh(actor, new ConvergingFireStatusEffect(), Remaining(state.RepeatedTargetExpires, now),
                state.HasRepeatedTargetBonus(now, configuration));
            Refresh(actor, new ChangingAnglesStatusEffect(), Remaining(state.MovementAccuracyExpires, now),
                state.MovementAccuracyExpires > now);
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

