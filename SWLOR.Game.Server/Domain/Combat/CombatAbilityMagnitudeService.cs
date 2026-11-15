using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// How Force affinity and combat readiness scale an ability's magnitude (part 4 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from <c>AbilityService</c> unchanged. It reads the active impact
    /// from <see cref="IAbilityImpactState"/> and the perk and stat rules from Character, all below Combat, so the ability effect
    /// helpers and the ability damage formulas share it. The calls into the perk and stat services sit behind one-line
    /// <c>protected virtual</c> methods so a test can answer them.
    /// </summary>
    public class CombatAbilityMagnitudeService : ICombatAbilityMagnitudeService
    {
        private readonly IAbilityImpactState _impactState;
        private readonly IPerkService _perk;
        private readonly IStatService _stat;

        public CombatAbilityMagnitudeService(IAbilityImpactState impactState, IPerkService perk, IStatService stat)
        {
            _impactState = impactState;
            _perk = perk;
            _stat = stat;
        }

        public float GetActiveForceAffinityMagnitudeMultiplier(uint activator)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return 1f;

            return GetForceAffinityMagnitudeMultiplier(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType);
        }

        public float GetActiveForceAffinityRestorativeMagnitudeMultiplier(uint activator)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return 1f;

            return GetForceAffinityRestorativeMagnitudeMultiplier(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType);
        }

        public int ApplyActiveForceAffinityRestorativeMagnitude(uint activator, int amount)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return ApplyForceAffinityRestorativeMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public int ApplyActiveForceAffinityMagnitude(uint activator, int amount)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public float ApplyActiveForceAffinityMagnitude(uint activator, float amount)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int amount)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact == null)
                return amount;

            return ApplyCombatReadinessMagnitude(activator, amount);
        }

        public int ApplyCombatReadinessMagnitude(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var combatReadiness = GetCombatReadinessPercent(creature);
            if (combatReadiness > 0)
            {
                amount += (int)Math.Ceiling(amount * (combatReadiness / 100f));
            }

            return amount;
        }

        protected virtual int GetCombatReadinessPercent(uint creature) => _stat.GetCombatReadinessPercent(creature);

        protected virtual float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType) => _perk.GetForceAffinityMagnitudeMultiplier(creature, perkType);

        protected virtual float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType) => _perk.GetForceAffinityRestorativeMagnitudeMultiplier(creature, perkType);

        protected virtual int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount) => _perk.ApplyForceAffinityRestorativeMagnitude(creature, perkType, amount);

        protected virtual int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount) => _perk.ApplyForceAffinityMagnitude(creature, perkType, amount);

        protected virtual float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount) => _perk.ApplyForceAffinityMagnitude(creature, perkType, amount);
    }
}
