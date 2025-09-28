using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for campaign variables
        private readonly Dictionary<string, Dictionary<string, Dictionary<uint, object>>> _campaignData = new();
        private readonly Dictionary<string, Dictionary<string, Dictionary<uint, uint>>> _campaignObjects = new();

        private string GetKey(string sCampaignName, string sVarName, uint oPlayer) => $"{sCampaignName}|{sVarName}|{oPlayer}";

        public void SetCampaignFloat(string sCampaignName, string sVarName, float flFloat, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = flFloat;
        }

        public void SetCampaignInt(string sCampaignName, string sVarName, int nInt, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = nInt;
        }

        public void SetCampaignVector(string sCampaignName, string sVarName, Vector3 vVector, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = vVector;
        }

        public void SetCampaignLocation(string sCampaignName, string sVarName, Location locLocation, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = locLocation;
        }

        public void SetCampaignString(string sCampaignName, string sVarName, string sString, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = sString;
        }

        public void DestroyCampaignDatabase(string sCampaignName)
        {
            _campaignData.Remove(sCampaignName);
            _campaignObjects.Remove(sCampaignName);
        }

        public float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is float value)
                return value;
            return 0.0f;
        }

        public int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is int value)
                return value;
            return 0;
        }

        public Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is Vector3 value)
                return value;
            return new Vector3(0, 0, 0);
        }

        public Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is Location value)
                return value;
            return new Location(0);
        }

        public string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is string value)
                return value;
            return "";
        }

        public void DeleteCampaignVariable(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName].Remove(oPlayer);
        }

        public int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject, uint oPlayer = OBJECT_INVALID, bool bSaveObjectState = false)
        {
            if (!_campaignObjects.ContainsKey(sCampaignName))
                _campaignObjects[sCampaignName] = new Dictionary<string, Dictionary<uint, uint>>();
            if (!_campaignObjects[sCampaignName].ContainsKey(sVarName))
                _campaignObjects[sCampaignName][sVarName] = new Dictionary<uint, uint>();
            _campaignObjects[sCampaignName][sVarName][oPlayer] = oObject;
            return 1; // Success
        }

        public uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation, uint oOwner = OBJECT_INVALID, uint oPlayer = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            if (_campaignObjects.ContainsKey(sCampaignName) && 
                _campaignObjects[sCampaignName].ContainsKey(sVarName) && 
                _campaignObjects[sCampaignName][sVarName].ContainsKey(oPlayer))
                return _campaignObjects[sCampaignName][sVarName][oPlayer];
            return OBJECT_INVALID;
        }

        public void SetCampaignJson(string sCampaignName, string sVarName, Json jValue, uint oPlayer = OBJECT_INVALID)
        {
            if (!_campaignData.ContainsKey(sCampaignName))
                _campaignData[sCampaignName] = new Dictionary<string, Dictionary<uint, object>>();
            if (!_campaignData[sCampaignName].ContainsKey(sVarName))
                _campaignData[sCampaignName][sVarName] = new Dictionary<uint, object>();
            _campaignData[sCampaignName][sVarName][oPlayer] = jValue;
        }

        public Json GetCampaignJson(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            if (_campaignData.ContainsKey(sCampaignName) && 
                _campaignData[sCampaignName].ContainsKey(sVarName) && 
                _campaignData[sCampaignName][sVarName].ContainsKey(oPlayer) &&
                _campaignData[sCampaignName][sVarName][oPlayer] is Json value)
                return value;
            return new Json(0);
        }

        // Helper methods for testing

    }
}
