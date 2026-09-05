using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Shares once-per-cast triggers across delayed shapes and repeated field impacts.
    /// </summary>
    public sealed class AbilityImpactSequence
    {
        private bool _areaPulseTriggered;
        private readonly HashSet<uint> _chainTargets = new();

        public bool TryConsumeChainArc(uint target, int maximumTargets)
        {
            if (_chainTargets.Count >= maximumTargets)
                return false;

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
