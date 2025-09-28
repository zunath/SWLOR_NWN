using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for traps and calendar
        private int _calendarYear = 1372;
        private int _calendarMonth = 1;
        private int _calendarDay = 1;
        private readonly Dictionary<uint, TrapData> _trapData = new();
        private readonly Dictionary<uint, HashSet<uint>> _trapDetectors = new();
        private uint _nearestTrap = OBJECT_INVALID;
        private uint _lastTrapDetected = OBJECT_INVALID;

        private class TrapData
        {
            public bool IsTrapped { get; set; } = false;
            public bool IsDisarmable { get; set; } = true;
            public bool IsDetectable { get; set; } = true;
            public bool IsFlagged { get; set; } = false;
            public TrapBaseType BaseType { get; set; } = TrapBaseType.MinorSpike;
            public bool IsOneShot { get; set; } = false;
            public uint Creator { get; set; } = OBJECT_INVALID;
            public string KeyTag { get; set; } = "";
            public int DisarmDC { get; set; } = 20;
            public int DetectDC { get; set; } = 20;
            public bool IsActive { get; set; } = true;
            public bool IsRecoverable { get; set; } = false;
        }

        public void SetCalendar(int nYear, int nMonth, int nDay) 
        {
            _calendarYear = nYear;
            _calendarMonth = Math.Max(1, Math.Min(12, nMonth));
            _calendarDay = Math.Max(1, Math.Min(31, nDay));
        }


        public int GetCalendarYear() => _calendarYear;

        public int GetCalendarMonth() => _calendarMonth;

        public int GetCalendarDay() => _calendarDay;

        public int SetTrapDetectedBy(uint oTrap, uint oDetector, bool bDetected = true) 
        {
            if (!_trapDetectors.ContainsKey(oTrap))
                _trapDetectors[oTrap] = new HashSet<uint>();
            
            if (bDetected)
                _trapDetectors[oTrap].Add(oDetector);
            else
                _trapDetectors[oTrap].Remove(oDetector);
            
            return 1; // Success
        }

        public bool GetIsTrapped(uint oObject) => 
            _trapData.GetValueOrDefault(oObject, new TrapData()).IsTrapped;

        public bool GetTrapDisarmable(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsDisarmable;

        public bool GetTrapDetectable(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsDetectable;

        public bool GetTrapDetectedBy(uint oTrapObject, uint oCreature) => 
            _trapDetectors.GetValueOrDefault(oTrapObject, new HashSet<uint>()).Contains(oCreature);

        public bool GetTrapFlagged(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsFlagged;

        public TrapBaseType GetTrapBaseType(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).BaseType;

        public bool GetTrapOneShot(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsOneShot;

        public uint GetTrapCreator(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).Creator;

        public string GetTrapKeyTag(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).KeyTag;

        public int GetTrapDisarmDC(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).DisarmDC;

        public int GetTrapDetectDC(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).DetectDC;

        public uint GetNearestTrapToObject(uint oTarget = OBJECT_INVALID, bool nTrapDetected = true) => _nearestTrap;

        public uint GetLastTrapDetected(uint oTarget = OBJECT_INVALID) => _lastTrapDetected;

        public bool GetTrapActive(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsActive;

        public void SetTrapActive(uint oTrapObject, bool nActive = true) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.IsActive = nActive;
        }

        public bool GetTrapRecoverable(uint oTrapObject) => 
            _trapData.GetValueOrDefault(oTrapObject, new TrapData()).IsRecoverable;

        public void SetTrapRecoverable(uint oTrapObject, bool nRecoverable = true) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.IsRecoverable = nRecoverable;
        }

        public void SetTrapDisarmable(uint oTrapObject, bool nDisarmable = true) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.IsDisarmable = nDisarmable;
        }

        public void SetTrapDetectable(uint oTrapObject, bool nDetectable = true) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.IsDetectable = nDetectable;
        }

        public void SetTrapOneShot(uint oTrapObject, bool nOneShot = true) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.IsOneShot = nOneShot;
        }

        public void SetTrapKeyTag(uint oTrapObject, string sKeyTag) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.KeyTag = sKeyTag;
        }

        public void SetTrapDisarmDC(uint oTrapObject, int nDisarmDC) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.DisarmDC = nDisarmDC;
        }

        public void SetTrapDetectDC(uint oTrapObject, int nDetectDC) 
        {
            var data = GetOrCreateTrapData(oTrapObject);
            data.DetectDC = nDetectDC;
        }

        public uint CreateTrapAtLocation(TrapBaseType nTrapType, Location lLocation, float fSize = 2.0f, string sTag = "", FactionType nFaction = FactionType.Hostile, string sOnDisarmScript = "", string sOnTrapTriggeredScript = "") 
        {
            var newTrap = (uint)(_trapData.Count + 13000);
            var data = GetOrCreateTrapData(newTrap);
            data.IsTrapped = true;
            data.BaseType = nTrapType;
            return newTrap;
        }

        public void CreateTrapOnObject(TrapBaseType nTrapType, uint oObject, FactionType nFaction = FactionType.Hostile, string sOnDisarmScript = "", string sOnTrapTriggeredScript = "") 
        {
            var data = GetOrCreateTrapData(oObject);
            data.IsTrapped = true;
            data.BaseType = nTrapType;
        }

        public void SetTrapDisabled(uint oTrap) 
        {
            var data = GetOrCreateTrapData(oTrap);
            data.IsActive = false;
        }

        private TrapData GetOrCreateTrapData(uint oTrap)
        {
            if (!_trapData.ContainsKey(oTrap))
                _trapData[oTrap] = new TrapData();
            return _trapData[oTrap];
        }

        // Helper methods for testing

    }
}
