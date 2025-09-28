using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for encounters
        private readonly Dictionary<uint, EncounterData> _encounterData = new();

        private class EncounterData
        {
            public bool IsActive { get; set; } = false;
            public int SpawnsMax { get; set; } = 0;
            public int SpawnsCurrent { get; set; } = 0;
            public EncounterDifficultyType Difficulty { get; set; } = EncounterDifficultyType.Low;
        }

        public int GetEncounterActive(uint oEncounter = OBJECT_INVALID) => 
            _encounterData.GetValueOrDefault(oEncounter, new EncounterData()).IsActive ? 1 : 0;

        public void SetEncounterActive(int nNewValue, uint oEncounter = OBJECT_INVALID) 
        {
            var data = GetOrCreateEncounterData(oEncounter);
            data.IsActive = nNewValue != 0;
        }

        public int GetEncounterSpawnsMax(uint oEncounter = OBJECT_INVALID) => 
            _encounterData.GetValueOrDefault(oEncounter, new EncounterData()).SpawnsMax;

        public void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = OBJECT_INVALID) 
        {
            var data = GetOrCreateEncounterData(oEncounter);
            data.SpawnsMax = nNewValue;
        }

        public int GetEncounterSpawnsCurrent(uint oEncounter = OBJECT_INVALID) => 
            _encounterData.GetValueOrDefault(oEncounter, new EncounterData()).SpawnsCurrent;

        public void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = OBJECT_INVALID) 
        {
            var data = GetOrCreateEncounterData(oEncounter);
            data.SpawnsCurrent = nNewValue;
        }

        public void SetEncounterDifficulty(EncounterDifficultyType nEncounterDifficulty, uint oEncounter = OBJECT_INVALID) 
        {
            var data = GetOrCreateEncounterData(oEncounter);
            data.Difficulty = nEncounterDifficulty;
        }

        public int GetEncounterDifficulty(uint oEncounter = OBJECT_INVALID) => 
            (int)_encounterData.GetValueOrDefault(oEncounter, new EncounterData()).Difficulty;

        private EncounterData GetOrCreateEncounterData(uint oEncounter)
        {
            if (!_encounterData.ContainsKey(oEncounter))
                _encounterData[oEncounter] = new EncounterData();
            return _encounterData[oEncounter];
        }

        // Helper methods for testing
    }
}
