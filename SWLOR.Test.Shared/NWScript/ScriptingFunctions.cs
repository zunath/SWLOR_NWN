using System.Collections.Generic;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for scripting
        private readonly Dictionary<string, string> _scriptParams = new();
        private EventScriptType _currentlyRunningEvent = EventScriptType.Invalid;
        private int _scriptInstructionsRemaining = 1000;
        private readonly List<string> _scriptBacktrace = new();
        private readonly Dictionary<string, int> _jmpLabels = new();
        private int _scriptRecursionLevel = 0;
        private readonly List<string> _scriptNames = new();
        private readonly List<string> _scriptChunks = new();

        public string GetScriptParam(string sParamName) => 
            _scriptParams.GetValueOrDefault(sParamName, "");

        public void SetScriptParam(string sParamName, string sParamValue) 
        {
            _scriptParams[sParamName] = sParamValue;
        }

        public EventScriptType GetCurrentlyRunningEvent(bool bInheritParent = true) => _currentlyRunningEvent;

        public int GetScriptInstructionsRemaining() => _scriptInstructionsRemaining;

        public string CompileScript(string sScriptName, string sScriptData, bool bWrapIntoMain = false, bool bGenerateNDB = false) 
        {
            // Mock implementation - return success
            return "Compiled successfully";
        }

        public void AbortRunningScript(string sError = "") 
        {
            // Mock implementation - no-op for testing
        }

        public Json GetScriptBacktrace(bool bIncludeStack = true) 
        {
            var backtrace = JsonArray();
            // Mock implementation - return empty array
            return backtrace;
        }

        public int SetJmp(string sLabel) 
        {
            _jmpLabels[sLabel] = 1; // Success
            return 1;
        }

        public void LongJmp(string sLabel, int nRetVal = 1) 
        {
            // Mock implementation - no-op for testing
        }

        public bool GetIsValidJmp(string sLabel) => _jmpLabels.ContainsKey(sLabel);

        public int GetScriptRecursionLevel() => _scriptRecursionLevel;

        public string GetScriptName(int nRecursionLevel = -1) 
        {
            if (nRecursionLevel == -1) nRecursionLevel = _scriptRecursionLevel;
            if (nRecursionLevel >= 0 && nRecursionLevel < _scriptNames.Count)
                return _scriptNames[nRecursionLevel];
            return "";
        }

        public string GetScriptChunk(int nRecursionLevel = -1) 
        {
            if (nRecursionLevel == -1) nRecursionLevel = _scriptRecursionLevel;
            if (nRecursionLevel >= 0 && nRecursionLevel < _scriptChunks.Count)
                return _scriptChunks[nRecursionLevel];
            return "";
        }

        // Helper methods for testing
    }
}
