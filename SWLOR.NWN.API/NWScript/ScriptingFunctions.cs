using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
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
            return global::NWN.Core.NWScript.GetScriptParam(sParamName);
        }

        /// <summary>
        /// Set a script parameter value for the next script to be run.
        /// Call this function to set parameters right before calling ExecuteScript().
        /// </summary>
        public static void SetScriptParam(string sParamName, string sParamValue)
        {
            global::NWN.Core.NWScript.SetScriptParam(sParamName, sParamValue);
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
            return (EventScript)global::NWN.Core.NWScript.GetCurrentlyRunningEvent(bInheritParent ? 1 : 0);
        }

        /// <summary>
        /// Returns the number of script instructions remaining for the currently-running script.
        /// Once this value hits zero, the script will abort with TOO MANY INSTRUCTIONS.
        /// The instruction limit is configurable by the user, so if you have a really long-running
        /// process, this value can guide you with splitting it up into smaller, discretely schedulable parts.
        /// Note: Running this command and checking/handling the value also takes up some instructions.
        /// </summary>
        /// <returns></returns>
        public static int GetScriptInstructionsRemaining()
        {
            return global::NWN.Core.NWScript.GetScriptInstructionsRemaining();
        }

        /// <summary>
        /// Compile a script and place it in the server's CURRENTGAME: folder.
        /// Note: Scripts will persist for as long as the module is running.
        /// SinglePlayer / Saves: Scripts that overwrite existing module scripts will persist to the save file.
        ///                       New scripts, unknown to the module, will have to be re-compiled on module load when loading a save.
        /// Returns "" on success or the error on failure.
        /// </summary>
        public static string CompileScript(string sScriptName, string sScriptData, bool bWrapIntoMain = false, bool bGenerateNDB = false)
        {
            return global::NWN.Core.NWScript.CompileScript(sScriptName, sScriptData, bWrapIntoMain ? 1 : 0, bGenerateNDB ? 1 : 0);
        }

        /// <summary>
        /// This immediately aborts the running script.
        /// - Will not emit an error to the server log by default.
        /// - You can specify the optional sError to emit as a script error, which will be printed
        ///   to the log and sent to all players, just like any other script error.
        /// - Will not terminate other script recursion (e.g. nested ExecuteScript()) will resume as if the
        ///   called script exited cleanly.
        /// - This call will never return.
        /// </summary>
        public static void AbortRunningScript(string sError = "")
        {
            global::NWN.Core.NWScript.AbortRunningScript(sError);
        }

        /// <summary>
        /// Generate a VM debug view into the current execution location.
        /// - Names and symbols can only be resolved if debug information is available (NDB file).
        /// - This call can be a slow call for large scripts.
        /// - Setting bIncludeStack = TRUE will include stack info in the output, which could be a
        ///   lot of data for large scripts. You can turn it off if you do not need the info.
        /// Returned data format (JSON object):
        ///   "frames": array of stack frames:
        ///     "ip": instruction pointer into code
        ///     "bp", "sp": current base/stack pointer
        ///     "file", "line", "function": available only if NDB loaded correctly
        ///   "stack": abbreviated stack data (only if bIncludeStack is TRUE)
        ///     "type": one of the nwscript object types, OR:
        ///     "type_unknown": hex code of AUX
        ///     "data": type-specific payload. Not all type info is rendered in the interest of brevity.
        ///             Only enough for you to re-identify which variable this might belong to.
        /// </summary>
        public static Json GetScriptBacktrace(bool bIncludeStack = true)
        {
            return global::NWN.Core.NWScript.GetScriptBacktrace(bIncludeStack ? 1 : 0);
        }

        /// <summary>
        /// Mark the current location in code as a jump target, identified by sLabel.
        /// - Returns 0 on initial invocation, but will return nRetVal if jumped-to by LongJmp.
        /// - sLabel can be any valid string (including empty); though it is recommended to pick
        ///   something distinct. The responsibility of namespacing lies with you.
        /// - Calling repeatedly with the same label will overwrite the previous jump location.
        ///   If you want to nest them, you need to manage nesting state externally.
        /// </summary>
        public static int SetJmp(string sLabel)
        {
            return global::NWN.Core.NWScript.SetJmp(sLabel);
        }

        /// <summary>
        /// Jump execution back in time to the point where you called SetJmp with the same label.
        /// - This function is a GREAT way to get really hard-to-debug stack under/overflows.
        /// - Will not work across script runs or script recursion; only within the same script.
        ///   (However, it WILL work across includes - those go into the same script data in compilation)
        /// - Will throw a script error if sLabel does not exist.
        /// - Will throw a script error if no valid jump destination exists.
        /// - You CAN jump to locations with compatible stack layout, including sibling functions.
        ///   For the script to successfully finish, the entire stack needs to be correct (either in code or
        ///   by jumping elsewhere again). Making sure this is the case is YOUR responsibility.
        /// - The parameter nRetVal is passed to SetJmp, resuming script execution as if SetJmp returned
        ///   that value (instead of 0).
        ///   If you accidentally pass 0 as nRetVal, it will be silently rewritten to 1.
        ///   Any other integer value is valid, including negative ones.
        /// - This call will never return.
        /// </summary>
        public static void LongJmp(string sLabel, int nRetVal = 1)
        {
            global::NWN.Core.NWScript.LongJmp(sLabel, nRetVal);
        }

        /// <summary>
        /// Returns TRUE if the given sLabel is a valid jump target at the current code location.
        /// </summary>
        public static bool GetIsValidJmp(string sLabel)
        {
            return global::NWN.Core.NWScript.GetIsValidJmp(sLabel) != 0;
        }

        /// <summary>
        /// Get the current script recursion level.
        /// </summary>
        public static int GetScriptRecursionLevel()
        {
            return global::NWN.Core.NWScript.GetScriptRecursionLevel();
        }

        /// <summary>
        /// Get the name of the script at a script recursion level.
        /// - nRecursionLevel: Between 0 and <= GetScriptRecursionLevel() or -1 for the current recursion level.
        /// Returns the script name or "" on error.
        /// </summary>
        public static string GetScriptName(int nRecursionLevel = -1)
        {
            return global::NWN.Core.NWScript.GetScriptName(nRecursionLevel);
        }

        /// <summary>
        /// Get the script chunk attached to a script recursion level.
        /// - nRecursionLevel: Between 0 and <= GetScriptRecursionLevel() or -1 for the current recursion level.
        /// Returns the script chunk or "" on error / no script chunk attached.
        /// </summary>
        public static string GetScriptChunk(int nRecursionLevel = -1)
        {
            return global::NWN.Core.NWScript.GetScriptChunk(nRecursionLevel);
        }
    }
}
