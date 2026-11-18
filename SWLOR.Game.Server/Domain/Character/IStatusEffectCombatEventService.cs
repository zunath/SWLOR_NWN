using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effects reacting to combat (part 6 of the status effect split): the damage and attack notices, the guarded hit, the
    /// native attack swing with its deferred applications, and the limited attack-timing reads.
    /// </summary>
    public interface IStatusEffectCombatEventService
    {
        /// <summary>Whether a limited attack-timing effect granted to a creature now must wait for its native attack cycle to end.</summary>
        bool IsDeferredNativeAttackStatusEffect(IStatusEffect statusEffect, uint creature);

        /// <summary>Holds an application until the outermost native attack cycle of the creature ends, and runs it then.</summary>
        void DeferNativeAttackStatusEffect(uint creature, Action applyDeferredEffect);

        void OnDealtDamage();

        void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null);

        /// <summary>
        /// Marks the start of a native attack cycle. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost cycle ends.
        /// Dual-wield cycles retain this scope across the gap between hands.
        /// </summary>
        void BeginNativeAttackSwing(uint attacker);

        /// <summary>
        /// Discards a destroyed attacker's open native attack cycle and its deferred effects without applying them,
        /// so a reused object ID never starts inside a stale cycle.
        /// </summary>
        void ForgetDestroyedNativeAttackSwing(uint attacker);

        /// <summary>
        /// Ends (or cancels) a native attack cycle and applies limited attack-timing effects
        /// granted while its precomputed rolls resolved.
        /// </summary>
        void EndNativeAttackSwing(uint attacker);

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks);

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks);

        void OnGuardedHit(uint defender, uint attacker, int preventedDamage);
    }
}
