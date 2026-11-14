using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Engine adapter for stat-configured weapon traits and captured ability charges.</summary>
    public interface IPairedRangedTraitsService
    {
        /// <summary>Reports a weapon ability's first landed hit; its chain or own grant adds Cadence once.</summary>
        void RecordWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation);

        WeaponAbilityTraitActivation CaptureWeaponAbility(uint actor, AbilityDetail ability);

        void CommitWeaponAbility(uint actor, AbilityDetail ability, WeaponAbilityTraitActivation activation);

        void CompleteWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation);

        void CompleteIntrinsicAbility(uint actor, WeaponAbilityTraitActivation activation, bool anyHit, int paidStamina, int restoreStamina, int bonusAttempts, int bonusDamagePercent, int bonusSeconds);

        PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(uint actor, uint target, long rollId);

        void RecordOrdinaryHit(uint actor, uint target, long rollId);

        int GetOrdinaryDamagePercentAdjustment(uint actor, uint target);

        int GetOrdinaryAccuracy(uint actor);

        void RecordTargetChange(uint actor, uint target);

        /// <summary>Lets callers skip mitigation work for defenders that have no direct-damage trait.</summary>
        bool HasDirectHostileDamageTrait(uint actor);

        /// <summary>Only positive direct hostile damage may call this hook.</summary>
        void RecordDirectHostileDamage(uint actor);

        /// <summary>Only a direct hostile accuracy-versus-evasion miss may call this hook.</summary>
        void RecordAccuracyEvasionMiss(uint actor);

        int GetAbilityStaminaCostAdjustment(uint actor);

        int ConsumeAbilityStaminaDiscount(uint actor);

        PairedRangedAbilityTraitsSnapshot BeginAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence);

        PairedRangedAbilityTraitsSnapshot CaptureAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence);

        bool CommitAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot);

        void CompleteAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot, bool anyHit, int paidStamina, int restoreStaminaOnHit, int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0);

        void Reconcile(uint actor);

        void ExitExtendedCadence(uint actor);

        void ClearCombatState(uint actor);

        void ClearEquipmentState(uint actor);

        void ClearActor(uint actor);

        /// <summary>Drops tracking for a destroyed object without refreshing its status effects.</summary>
        void ForgetDestroyed(uint actor);

        PairedRangedTraitConfiguration GetConfiguration(uint actor);
    }
}
