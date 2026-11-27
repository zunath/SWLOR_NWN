using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The lifecycle of each ability impact (part 5 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md): begin,
    /// the recurring and batched impacts a cast captures, end and abort. It writes the impact state store
    /// (<see cref="IAbilityImpactState"/>) the Combat services read.
    /// </summary>
    public interface IAbilityImpactLifecycleService
    {
        /// <summary>
        /// Starts impact tracking and defers pending damage bonuses until a damaging payload,
        /// retaining activation-marker snapshots for the impact footprint and flash decision.
        /// </summary>
        void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            bool countsAsAttackAttempt = true,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            uint triggeringWeapon = OBJECT_INVALID,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null,
            WeaponAbilityTraitActivation weaponTraits = null);

        /// <summary>
        /// Retains the originating ability and cast sequence for recurring impacts without
        /// consuming another activation's pending bonuses or replacing its impact tracker.
        /// </summary>
        Action CaptureRepeatedAbilityImpact(
            uint activator, Action impactAction, int baseDamage = 0, bool requiresLivingActivator = true);

        /// <summary>
        /// Captures one cast's recurring impact context while allowing each delayed impact
        /// to retain its own target or payload. Bonuses are consumed only when an impact lands.
        /// </summary>
        Action<T> CaptureRepeatedAbilityImpact<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0, bool requiresLivingActivator = true,
            bool retainCapturedBonuses = false);

        Func<int, Action<T>> CaptureRepeatedAbilityImpactBatch<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0);

        AbilityImpactSummary EndAbilityImpact(uint activator);

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        void AbortAbilityImpact(uint activator);

        /// <summary>
        /// Begins the impact of a telegraph that resolves after its cast, with the bonuses the cast captured and the status-applied
        /// bonus the activator holds when it lands; the bonuses the cast left pending resolve from its impact at the first
        /// damaging payload.
        /// </summary>
        void BeginTelegraphedAbilityImpact(
            uint creator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            AbilityImpactTracker sequenceOwner,
            bool resolveDamageBonusesFromOwner);

        /// <summary>
        /// Resolves the damage bonuses the activator's impact deferred, when a payload of <paramref name="baseDamage"/> can deal
        /// damage (<see cref="CombatRules.CanDealCombatImpactDamage"/>).
        /// </summary>
        void PrepareCombatImpactDamageBonuses(uint activator, int baseDamage);
    }
}
