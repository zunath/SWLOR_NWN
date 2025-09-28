namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for placeables
        private readonly Dictionary<uint, PlaceableData> _placeableData = new();
        private uint _lastUsedBy = OBJECT_INVALID;

        private class PlaceableData
        {
            public bool IsIlluminated { get; set; } = false;
            public HashSet<int> PossibleActions { get; set; } = new();
        }

        public void ActionInteractObject(uint oPlaceable) 
        {
            // Mock implementation - no-op for testing
        }

        public uint GetLastUsedBy() => _lastUsedBy;

        public void SetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID, bool bIlluminate = true) 
        {
            var data = GetOrCreatePlaceableData(oPlaceable);
            data.IsIlluminated = bIlluminate;
        }

        public bool GetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID) => 
            _placeableData.GetValueOrDefault(oPlaceable, new PlaceableData()).IsIlluminated;

        public int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction) 
        {
            var data = _placeableData.GetValueOrDefault(oPlaceable, new PlaceableData());
            return data.PossibleActions.Contains(nPlaceableAction) ? 1 : 0;
        }

        public void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction) 
        {
            // Mock implementation - no-op for testing
        }

        private PlaceableData GetOrCreatePlaceableData(uint oPlaceable)
        {
            if (!_placeableData.ContainsKey(oPlaceable))
                _placeableData[oPlaceable] = new PlaceableData();
            return _placeableData[oPlaceable];
        }

        // Helper methods for testing

    }
}
