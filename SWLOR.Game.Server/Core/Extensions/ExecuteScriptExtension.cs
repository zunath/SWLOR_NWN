// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Make oTarget run sScript and then return execution to the calling script.
        ///   If sScript does not specify a compiled script, nothing happens.
        /// </summary>
        /// <summary>
        ///   Make oTarget run sScript and then return execution to the calling script.
        ///   If sScript does not specify a compiled script, nothing happens.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            NWNCore.NativeFunctions.StackPushObject(oTarget);
            NWNCore.NativeFunctions.StackPushStringUTF8(sScript);
            NWNCore.NativeFunctions.CallBuiltIn(8);
        }

    }
}
