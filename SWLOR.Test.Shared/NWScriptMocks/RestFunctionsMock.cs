using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for rest functions
        private readonly Dictionary<uint, bool> _restingCreatures = new();
        private uint _lastPCRested = OBJECT_INVALID;
        private RestEventType _lastRestEventType = RestEventType.Invalid;

        public void ForceRest(uint oCreature) 
        {
            _restingCreatures[oCreature] = true;
        }

        public bool GetIsResting(uint oCreature = OBJECT_INVALID) => 
            _restingCreatures.GetValueOrDefault(oCreature, false);

        public uint GetLastPCRested() => _lastPCRested;

        public RestEventType GetLastRestEventType() => _lastRestEventType;

        public void ActionRest(bool bCreatureToEnemyLineOfSightCheck = false) 
        {
            _restingCreatures[OBJECT_SELF] = true;
        }

        // Helper methods for testing
    }
}
