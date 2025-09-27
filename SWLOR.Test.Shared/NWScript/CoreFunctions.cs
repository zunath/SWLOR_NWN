using System.Collections.Generic;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Constants
        public const uint OBJECT_INVALID = 0x7F000000;
        public const uint OBJECT_SELF = 0x7F000001;
        public const int USE_CREATURE_LEVEL = -1;
        
        // Mock data storage
        private readonly Dictionary<string, object> _storedValues = new();
        private readonly List<string> _printHistory = new();
        private uint _moduleObject = OBJECT_INVALID;
        private int _randomSeed = 0;

        public int Random(int nMaxInteger) 
        {
            _randomSeed = (_randomSeed + 1) % nMaxInteger;
            return _randomSeed;
        }

        public uint GetModule() => _moduleObject;

        public void PrintString(string sString) 
        {
            _printHistory.Add($"String: {sString}");
        }

        public void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9) 
        {
            _printHistory.Add($"Float: {fFloat} (Width: {nWidth}, Decimals: {nDecimals})");
        }

        public void PrintInteger(int nInteger) 
        {
            _printHistory.Add($"Integer: {nInteger}");
        }

        public void PrintObject(uint oObject) 
        {
            _printHistory.Add($"Object: {oObject}");
        }

        // Helper methods for testing
        public List<string> GetPrintHistory() => _printHistory;
        public void ClearPrintHistory() => _printHistory.Clear();
        public void SetModuleObject(uint moduleObject) => _moduleObject = moduleObject;
    }
}
