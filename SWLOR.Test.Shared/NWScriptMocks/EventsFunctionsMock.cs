using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for events
        private readonly List<EventSignalRecord> _eventSignals = new();
        private readonly Dictionary<uint, Dictionary<EventScriptType, string>> _eventScripts = new();
        private int _userDefinedEventNumber = 0;
        private uint _lastClosedBy = OBJECT_INVALID;
        private Vector3 _lastGuiEventVector = new Vector3(0, 0, 0);

        private class EventSignalRecord
        {
            public uint Object { get; set; }
            public Event Event { get; set; }
        }

        public void SignalEvent(uint oObject, Event evToRun) 
        {
            _eventSignals.Add(new EventSignalRecord 
            { 
                Object = oObject, 
                Event = evToRun 
            });
        }

        public Event EventUserDefined(int nUserDefinedEventNumber) 
        {
            _userDefinedEventNumber = nUserDefinedEventNumber;
            return new Event(0);
        }

        public int GetUserDefinedEventNumber() => _userDefinedEventNumber;

        public uint GetLastClosedBy() => _lastClosedBy;

        public string GetEventScript(uint oObject, EventScriptType nHandler) => 
            _eventScripts.GetValueOrDefault(oObject, new Dictionary<EventScriptType, string>())
                .GetValueOrDefault(nHandler, "");

        public int SetEventScript(uint oObject, EventScriptType nHandler, string sScript) 
        {
            if (!_eventScripts.ContainsKey(oObject))
                _eventScripts[oObject] = new Dictionary<EventScriptType, string>();
            _eventScripts[oObject][nHandler] = sScript;
            return 1; // Success
        }

        public Vector3 GetLastGuiEventVector() => _lastGuiEventVector;

        // Helper methods for testing
    }
}
