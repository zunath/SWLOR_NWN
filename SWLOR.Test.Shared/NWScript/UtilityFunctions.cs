using System;
using System.Collections.Generic;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for utility functions
        private readonly Dictionary<uint, string> _objectUUIDs = new();
        private readonly Dictionary<string, uint> _uuidToObject = new();

        public string IntToHexString(int nInteger) => nInteger.ToString("X");

        public void SpawnScriptDebugger() 
        {
            // Mock implementation - no-op for testing
        }

        public string ExecuteScriptChunk(string sScriptChunk, uint oObject = OBJECT_INVALID, bool bWrapIntoMain = true) 
        {
            // Mock implementation - return success
            return "Script executed successfully";
        }

        public string GetRandomUUID() 
        {
            return Guid.NewGuid().ToString();
        }

        public string GetObjectUUID(uint oObject) 
        {
            if (_objectUUIDs.ContainsKey(oObject))
                return _objectUUIDs[oObject];
            
            var uuid = Guid.NewGuid().ToString();
            _objectUUIDs[oObject] = uuid;
            _uuidToObject[uuid] = oObject;
            return uuid;
        }

        public void ForceRefreshObjectUUID(uint oObject) 
        {
            if (_objectUUIDs.ContainsKey(oObject))
            {
                var oldUuid = _objectUUIDs[oObject];
                _uuidToObject.Remove(oldUuid);
            }
            
            var newUuid = Guid.NewGuid().ToString();
            _objectUUIDs[oObject] = newUuid;
            _uuidToObject[newUuid] = oObject;
        }

        public uint GetObjectByUUID(string sUUID) => 
            _uuidToObject.GetValueOrDefault(sUUID, OBJECT_INVALID);

        public void Reserved899() 
        {
            // Reserved function - no-op
        }

        // Helper methods for testing

    }
}
