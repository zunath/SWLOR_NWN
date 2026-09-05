using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Shares once-per-cast triggers across delayed shapes and repeated field impacts.
    /// </summary>
    public sealed class AbilityImpactSequence
    {
        private bool _areaPulseTriggered;
        private HashSet<uint> _chainTargets;

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
