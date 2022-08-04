// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Make oTarget run sScript and then return execution to the calling script.
        ///   If sScript does not specify a compiled script, nothing happens.
        /// </summary>
        public static void ExecuteScriptCS(string sScript, uint oTarget)
        {
            // A workaround for adjusting OBJECT_SELF is needed here because without it,
            // it will occasionally become invalid causing a cascading chain of errors to occur.
            // This likely has to do with the execution chain of C# -> NWScript -> C# -> C#
            // somewhere along the way, OBJECT_SELF gets out of whack.
            var oldObjectSelf = OBJECT_SELF;
            OBJECT_SELF = oTarget;
            // Note: Bypass the NWScript round-trip and directly call the script execution.
            Internal.DirectRunScript(sScript, oTarget);
            OBJECT_SELF = oldObjectSelf;
        }

    }
}
