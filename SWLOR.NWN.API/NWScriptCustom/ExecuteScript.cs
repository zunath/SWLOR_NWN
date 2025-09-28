// ReSharper disable once CheckNamespace

using SWLOR.NWN.API.Service;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptService
    {
        public void ExecuteNWScript(string sScript, uint oTarget)
        {
            global::NWN.Core.NWScript.ExecuteScript(sScript, oTarget);
        }
    }
}