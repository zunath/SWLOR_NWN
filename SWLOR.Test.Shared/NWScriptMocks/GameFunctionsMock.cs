using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for game state
        private Location _startingLocation = new Location(0);
        private string _moduleName = "";
        private int _gameDifficulty = 0;
        private int _moduleXPScale = 1;
        private readonly List<string> _logEntries = new();
        private readonly Dictionary<uint, ActionType> _currentActions = new();
        private bool _gameActivePause = false;
        private int _gamePauseState = 0;
        private int _tickRate = 0;
        private readonly Dictionary<string, int> _journalQuestExperience = new();
        private readonly Dictionary<string, int> _stringHashes = new();
        private int _microsecondCounter = 0;

        public Location GetStartingLocation() => _startingLocation;

        public string GetModuleName() => _moduleName;

        public void StartNewModule(string sModuleName) 
        {
            _moduleName = sModuleName;
        }

        public void DoSinglePlayerAutoSave() 
        {
            // Mock implementation - no-op for testing
        }

        public int GetGameDifficulty() => _gameDifficulty;

        public void EndGame(string sEndMovie) 
        {
            // Mock implementation - no-op for testing
        }

        public int GetModuleXPScale() => _moduleXPScale;

        public void SetModuleXPScale(int nXPScale) => _moduleXPScale = nXPScale;

        public void WriteTimestampedLogEntry(string sLogEntry) 
        {
            _logEntries.Add(sLogEntry);
        }

        public ActionType GetCurrentAction(uint oObject = OBJECT_INVALID) => 
            _currentActions.GetValueOrDefault(oObject, ActionType.MoveToPoint);

        public void SetGameActivePause(bool bState) => _gameActivePause = bState;

        public int GetGamePauseState() => _gamePauseState;

        public int GetTickRate() => _tickRate;

        public int GetJournalQuestExperience(string szPlotID) => 
            _journalQuestExperience.GetValueOrDefault(szPlotID, 0);

        public int HashString(string sString) 
        {
            if (!_stringHashes.ContainsKey(sString))
                _stringHashes[sString] = sString.GetHashCode();
            return _stringHashes[sString];
        }

        public int GetMicrosecondCounter() => _microsecondCounter;

        public void ActivatePortal(uint oTarget, string sIPaddress = "", string sPassword = "", string sWaypointTag = "", bool bSeamless = false) 
        {
            // Mock implementation - no-op for testing
        }

        // Helper methods for testing
    }
}
