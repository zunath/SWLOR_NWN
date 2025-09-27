using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for local variables
        private readonly Dictionary<uint, Dictionary<string, object>> _localVariables = new();

        public int GetLocalInt(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is int intValue)
                return intValue;
            return 0;
        }

        public bool GetLocalBool(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is bool boolValue)
                return boolValue;
            return false;
        }

        public float GetLocalFloat(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is float floatValue)
                return floatValue;
            return 0.0f;
        }

        public string GetLocalString(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is string stringValue)
                return stringValue;
            return "";
        }

        public uint GetLocalObject(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is uint objectValue)
                return objectValue;
            return OBJECT_INVALID;
        }

        public void SetLocalInt(uint oObject, string sVarName, int nValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = nValue;
        }

        public void SetLocalBool(uint oObject, string sVarName, bool nValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = nValue;
        }

        public void SetLocalFloat(uint oObject, string sVarName, float fValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = fValue;
        }

        public void SetLocalString(uint oObject, string sVarName, string sValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = sValue ?? "";
        }

        public void SetLocalObject(uint oObject, string sVarName, uint oValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = oValue;
        }

        public void SetLocalLocation(uint oObject, string sVarName, Location lValue) 
        {
            if (!_localVariables.ContainsKey(oObject))
                _localVariables[oObject] = new Dictionary<string, object>();
            _localVariables[oObject][sVarName] = lValue;
        }

        public Location GetLocalLocation(uint oObject, string sVarName) 
        {
            var objVars = _localVariables.GetValueOrDefault(oObject, new Dictionary<string, object>());
            if (objVars.ContainsKey(sVarName) && objVars[sVarName] is Location locationValue)
                return locationValue;
            return new Location(0);
        }

        public void DeleteLocalInt(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        public void DeleteLocalBool(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        public void DeleteLocalFloat(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        public void DeleteLocalString(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        public void DeleteLocalObject(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        public void DeleteLocalLocation(uint oObject, string sVarName) 
        {
            if (_localVariables.ContainsKey(oObject))
                _localVariables[oObject].Remove(sVarName);
        }

        // Helper methods for testing
    }
}
