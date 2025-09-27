using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for resource management
        private readonly Dictionary<string, Dictionary<ResType, string>> _resourceAliases = new();
        private readonly Dictionary<string, Dictionary<ResType, string>> _resourceContents = new();
        private readonly Dictionary<string, List<string>> _resourcePrefixes = new();

        public string ResManGetAliasFor(string sResRef, ResType nResType) 
        {
            return _resourceAliases.GetValueOrDefault(sResRef, new Dictionary<ResType, string>())
                .GetValueOrDefault(nResType, "");
        }

        public string ResManFindPrefix(string sPrefix, ResType nResType, int nNth = 1, bool bSearchBaseData = false, string sOnlyKeyTable = "") 
        {
            var key = $"{sPrefix}|{nResType}";
            var prefixes = _resourcePrefixes.GetValueOrDefault(key, new List<string>());
            if (nNth > 0 && nNth <= prefixes.Count)
                return prefixes[nNth - 1];
            return "";
        }

        public string ResManGetFileContents(string sResRef, int nResType) 
        {
            var resType = (ResType)nResType;
            return _resourceContents.GetValueOrDefault(sResRef, new Dictionary<ResType, string>())
                .GetValueOrDefault(resType, "");
        }

        // Helper methods for testing



    }
}
