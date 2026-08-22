using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Counts originating attacks instead of per-target damage callbacks. A single ability
    /// impact can notify damage status effects once per hit and target, but those callbacks
    /// still belong to the same attack for limited-attack buffs.
    /// </summary>
    public sealed class LimitedAttackCounter
    {
        private AbilityImpactSummary _lastAbilityImpact;
        private readonly AbilityImpactSummary _ignoredAbilityImpact;

        public int RemainingAttacks { get; private set; }

        public LimitedAttackCounter(
            int remainingAttacks,
            AbilityImpactSummary ignoredAbilityImpact = null)
        {
            RemainingAttacks = Math.Max(0, remainingAttacks);
            _ignoredAbilityImpact = ignoredAbilityImpact;
        }

        public bool TryConsume(AbilityImpactSummary activeAbilityImpact)
        {
            if (activeAbilityImpact != null &&
                ReferenceEquals(activeAbilityImpact, _ignoredAbilityImpact))
            {
                return false;
            }

            if (activeAbilityImpact != null &&
                ReferenceEquals(activeAbilityImpact, _lastAbilityImpact))
            {
                return false;
            }

            if (RemainingAttacks <= 0)
                return false;

            if (activeAbilityImpact != null)
                _lastAbilityImpact = activeAbilityImpact;

            RemainingAttacks--;
            return true;
        }

        public LimitedAttackCounter Clone()
        {
            var clone = new LimitedAttackCounter(
                RemainingAttacks,
                _ignoredAbilityImpact);
            clone._lastAbilityImpact = _lastAbilityImpact;
            return clone;
        }
    }
}
