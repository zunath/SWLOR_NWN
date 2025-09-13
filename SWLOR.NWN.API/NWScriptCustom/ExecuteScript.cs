using SWLOR.NWN.API.Core;

// ReSharper disable once CheckNamespace
namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Make oTarget run sScript and then return execution to the calling script.
        /// If sScript does not specify a compiled script, nothing happens.
        ///
        /// This command will make a round-trip back to the NWN context so it should only be used
        /// in situations where you are guaranteed to have an NWScript file in the module, for performance reasons.
        /// </summary>
        public static void ExecuteScriptNWScript(string sScript, uint oTarget)
        {
            NWNXPInvoke.StackPushObject(oTarget);
            NWNXPInvoke.StackPushString(sScript);
            NWNXPInvoke.CallBuiltIn(8);
        }

        /// <summary>
        /// Make oTarget run sScript and then return execution to the calling script.
        /// If sScript does not specify a compiled script, nothing happens.
        ///
        /// This command will bypass the NWN context. For this reason it can only execute C# event scripts.
        /// Use ExecuteScriptNWScript if you actually need to run something in the module.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            // todo: conversion to using NWN.Core needed here.


            // A workaround for adjusting OBJECT_SELF is needed here because without it,
            // it will occasionally become invalid causing a cascading chain of errors to occur.
            // This likely has to do with the execution chain of C# -> NWScript -> C# -> C#
            // somewhere along the way, OBJECT_SELF gets out of whack.
            //var oldObjectSelf = OBJECT_SELF;
            //OBJECT_SELF = oTarget;
            // Note: Bypass the NWScript round-trip and directly call the script execution.
            //Internal.DirectRunScript(sScript, oTarget);
            //OBJECT_SELF = oldObjectSelf;
        }
    }
}
