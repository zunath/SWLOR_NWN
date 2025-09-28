using SWLOR.NWN.API.Contracts;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock: INWScriptService
    {
        // Constants
        public const uint OBJECT_INVALID = 0x7F000000;
        public const int USE_CREATURE_LEVEL = -1;
        
        // Properties from INWScriptService
        public uint OBJECT_SELF { get; } = 0x7F000001;
        public int NumberOfInventorySlots { get; } = 18;
        public float PI { get; } = 3.14159265359f;
        public int EVENT_SPELL_CAST_AT { get; } = 2001;
        public int PORTRAIT_INVALID { get; } = 0;
        public string TILESET_RESREF_BEHOLDER_CAVES { get; } = "beholder_caves";
        public string TILESET_RESREF_CASTLE_INTERIOR { get; } = "castle_interior";
        public string TILESET_RESREF_CITY_EXTERIOR { get; } = "city_exterior";
        public string TILESET_RESREF_CITY_INTERIOR { get; } = "city_interior";
        public string TILESET_RESREF_CRYPT { get; } = "crypt";
        public string TILESET_RESREF_DESERT { get; } = "desert";
        public string TILESET_RESREF_DROW_INTERIOR { get; } = "drow_interior";
        public string TILESET_RESREF_DUNGEON { get; } = "dungeon";
        public string TILESET_RESREF_FOREST { get; } = "forest";
        public string TILESET_RESREF_FROZEN_WASTES { get; } = "frozen_wastes";
        public string TILESET_RESREF_ILLITHID_INTERIOR { get; } = "illithid_interior";
        public string TILESET_RESREF_MICROSET { get; } = "microset";
        public string TILESET_RESREF_MINES_AND_CAVERNS { get; } = "mines_and_caverns";
        public string TILESET_RESREF_RUINS { get; } = "ruins";
        public string TILESET_RESREF_RURAL { get; } = "rural";
        public string TILESET_RESREF_RURAL_WINTER { get; } = "rural_winter";
        public string TILESET_RESREF_SEWERS { get; } = "sewers";
        public string TILESET_RESREF_UNDERDARK { get; } = "underdark";
        public int EVENT_HEARTBEAT { get; } = 1001;
        public int EVENT_PERCEIVE { get; } = 1002;
        public int EVENT_END_COMBAT_ROUND { get; } = 1003;
        public int EVENT_DIALOGUE { get; } = 1004;
        public int EVENT_ATTACKED { get; } = 1005;
        public int EVENT_DAMAGED { get; } = 1006;
        public int EVENT_DISTURBED { get; } = 1007;
        
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
