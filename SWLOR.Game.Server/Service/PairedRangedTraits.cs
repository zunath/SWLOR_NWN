using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Engine adapter for stat-configured weapon traits and captured ability charges. The logic lives in
    /// <see cref="IPairedRangedTraitsService"/>; every member forwards to the container's instance.
    /// </summary>
    public static class PairedRangedTraits
    {
        private static IPairedRangedTraitsService Instance => FacadeAccessor.Resolve<IPairedRangedTraitsService>();

        /// <summary>Reports a weapon ability's first landed hit; its chain or own grant adds Cadence once.</summary>
        public static void RecordWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation) =>
            Instance.RecordWeaponAbilityHit(actor, activation);

        public static WeaponAbilityTraitActivation CaptureWeaponAbility(uint actor, AbilityDetail ability) =>
            Instance.CaptureWeaponAbility(actor, ability);

        public static void CommitWeaponAbility(uint actor, AbilityDetail ability, WeaponAbilityTraitActivation activation) =>
            Instance.CommitWeaponAbility(actor, ability, activation);

        public static void CompleteWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation) =>
            Instance.CompleteWeaponAbilityHit(actor, activation);

        public static void CompleteIntrinsicAbility(uint actor, WeaponAbilityTraitActivation activation, bool anyHit, int paidStamina, int restoreStamina, int bonusAttempts, int bonusDamagePercent, int bonusSeconds) =>
            Instance.CompleteIntrinsicAbility(actor, activation, anyHit, paidStamina, restoreStamina, bonusAttempts, bonusDamagePercent, bonusSeconds);

        public static PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(uint actor, uint target, long rollId) =>
            Instance.RecordOrdinaryAttempt(actor, target, rollId);

        public static void RecordOrdinaryHit(uint actor, uint target, long rollId) =>
            Instance.RecordOrdinaryHit(actor, target, rollId);

        public static int GetOrdinaryDamagePercentAdjustment(uint actor, uint target) =>
            Instance.GetOrdinaryDamagePercentAdjustment(actor, target);

        public static int GetOrdinaryAccuracy(uint actor) =>
            Instance.GetOrdinaryAccuracy(actor);

        public static void RecordTargetChange(uint actor, uint target) =>
            Instance.RecordTargetChange(actor, target);

        /// <summary>Lets callers skip mitigation work for defenders that have no direct-damage trait.</summary>
        public static bool HasDirectHostileDamageTrait(uint actor) =>
            Instance.HasDirectHostileDamageTrait(actor);

        /// <summary>Only positive direct hostile damage may call this hook.</summary>
        public static void RecordDirectHostileDamage(uint actor) =>
            Instance.RecordDirectHostileDamage(actor);

        /// <summary>Only a direct hostile accuracy-versus-evasion miss may call this hook.</summary>
        public static void RecordAccuracyEvasionMiss(uint actor) =>
            Instance.RecordAccuracyEvasionMiss(actor);

        public static int GetAbilityStaminaCostAdjustment(uint actor) =>
            Instance.GetAbilityStaminaCostAdjustment(actor);

        public static int ConsumeAbilityStaminaDiscount(uint actor) =>
            Instance.ConsumeAbilityStaminaDiscount(actor);

        public static PairedRangedAbilityTraitsSnapshot BeginAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence) =>
            Instance.BeginAbility(actor, usesNextAbilityAccuracy, spendsCadence);

        public static PairedRangedAbilityTraitsSnapshot CaptureAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence) =>
            Instance.CaptureAbility(actor, usesNextAbilityAccuracy, spendsCadence);

        public static bool CommitAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot) =>
            Instance.CommitAbility(actor, snapshot);

        public static void CompleteAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot, bool anyHit, int paidStamina, int restoreStaminaOnHit, int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0) =>
            Instance.CompleteAbility(actor, snapshot, anyHit, paidStamina, restoreStaminaOnHit, ordinaryBonusAttempts, ordinaryBonusDamagePercent, ordinaryBonusSeconds);

        public static void Reconcile(uint actor) =>
            Instance.Reconcile(actor);

        public static void ExitExtendedCadence(uint actor) =>
            Instance.ExitExtendedCadence(actor);

        public static void ClearCombatState(uint actor) =>
            Instance.ClearCombatState(actor);

        public static void ClearEquipmentState(uint actor) =>
            Instance.ClearEquipmentState(actor);

        public static void ClearActor(uint actor) =>
            Instance.ClearActor(actor);

        /// <summary>Drops tracking for a destroyed object without refreshing its status effects.</summary>
        public static void ForgetDestroyed(uint actor) =>
            Instance.ForgetDestroyed(actor);

        public static PairedRangedTraitConfiguration GetConfiguration(uint actor) =>
            Instance.GetConfiguration(actor);
    }
}
