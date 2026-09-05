namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Shares once-per-cast triggers across delayed shapes and repeated field impacts.
    /// </summary>
    public sealed class AbilityImpactSequence
    {
        private bool _areaPulseTriggered;
        private bool _chainTriggered;

        public bool TryTriggerChain()
        {
            if (_chainTriggered)
                return false;

            _chainTriggered = true;
            return true;
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
