// ReSharper disable once CheckNamespace

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