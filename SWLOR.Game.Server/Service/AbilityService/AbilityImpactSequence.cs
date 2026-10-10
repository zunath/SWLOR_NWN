using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Shares once-per-cast triggers across delayed shapes and repeated field impacts.
    /// </summary>
    public sealed class AbilityImpactSequence
    {
        private bool _areaPulseTriggered;
        private bool _partyBuffTriggered;
        private HashSet<uint> _chainTargets;
        private readonly HashSet<string> _damageRiders = new();
        private int? _damageHealingMaximum;
        private int _damageHealingApplied;

        /// <summary>Explicit drain healing shares one allowance across every target and phase.</summary>
        public int TakeDamageDerivedHealing(int maximumHP, int requested)
        {
            if (requested <= 0)
                return 0;

            _damageHealingMaximum ??= Combat.CalculateMaxHPHealingBudget(
                maximumHP, Combat.MaximumActivatedDamageHealingMaxHPPercent);
            var amount = Math.Clamp(requested, 0, Math.Max(0, _damageHealingMaximum.Value - _damageHealingApplied));
            _damageHealingApplied += amount;
            return amount;
        }

        /// <summary>Consumes one source's damage rider across all targets and phases of this cast.</summary>
        public bool TryTriggerDamageRider(string sourceKey) => _damageRiders.Add(sourceKey);

        public bool TryTriggerPartyBuff()
        {
            if (_partyBuffTriggered)
                return false;

            _partyBuffTriggered = true;
            return true;
        }

        public bool HasRemainingChainArcs(int maximumTargets) => (_chainTargets?.Count ?? 0) < maximumTargets;

        public bool TryConsumeChainArc(uint target, int maximumTargets)
        {
            if (!HasRemainingChainArcs(maximumTargets))
                return false;

            _chainTargets ??= new HashSet<uint>();
            return _chainTargets.Add(target);
        }

        public bool TryTriggerAreaPulse()
        {
            if (_areaPulseTriggered)
                return false;

            _areaPulseTriggered = true;
            return true;
        }
    }
}
