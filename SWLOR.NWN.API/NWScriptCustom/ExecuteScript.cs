// ReSharper disable once CheckNamespace
namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Make oTarget run sScript and then return execution to the calling script.
        /// If sScript does not specify a compiled script, nothing happens.
        ///
        /// This method automatically chooses between C# and NWScript execution:
        /// - If a C# script exists, it will be executed using proper VM context management
        /// - If no C# script exists, it falls back to NWScript execution
        ///
        /// This solves OBJECT_SELF corruption issues when executing C# scripts by bypassing
        /// the problematic NWScript round-trip.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            NWScriptRuntime.Current.ExecuteScript(sScript, oTarget);
        }
    }
}
