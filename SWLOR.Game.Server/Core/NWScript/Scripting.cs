using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Returns the script parameter value for a given parameter name.
        /// Script parameters can be set for conversation scripts in the toolset's
        /// Conversation Editor, or for any script with SetScriptParam().
        /// * Will return "" if a parameter with the given name does not exist.
        /// </summary>
        public static string GetScriptParam(string sParamName)
        {
            Internal.NativeFunctions.StackPushString(sParamName);
            Internal.NativeFunctions.CallBuiltIn(906);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        /// Set a script parameter value for the next script to be run.
        /// Call this function to set parameters right before calling ExecuteScript().
        /// </summary>
        public static void SetScriptParam(string sParamName, string sParamValue)
        {
            Internal.NativeFunctions.StackPushString(sParamValue);
            Internal.NativeFunctions.StackPushString(sParamName);
            Internal.NativeFunctions.CallBuiltIn(907);
        }

        /// <summary>
        ///  Returns the currently executing event (EVENT_SCRIPT_*) or 0 if not determinable.
        /// Note: Will return 0 in DelayCommand/AssignCommand.
        /// * bInheritParent: If TRUE, ExecuteScript(Chunk) will inherit their event ID from their parent event.
        ///                   If FALSE, it will return the event ID of the current script, which may be 0.
        ///
        /// Some events can run in the same script context as a previous event (for example: CreatureOnDeath, CreatureOnDamaged)
        /// In cases like these calling the function with bInheritParent = TRUE will return the wrong event ID.
        /// </summary>
        public static EventScript GetCurrentlyRunningEvent(bool bInheritParent = true)
        {
            Internal.NativeFunctions.StackPushInteger(bInheritParent ? 1 : 0);
            Internal.NativeFunctions.CallBuiltIn(938);
            return (EventScript)Internal.NativeFunctions.StackPopInteger();
        }

    }
}
