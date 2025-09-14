using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the starting location of the module.
        /// </summary>
        public static Location GetStartingLocation()
        {
            return global::NWN.Core.NWScript.GetStartingLocation();
        }

        /// <summary>
        ///   Get the module's name in the language of the server that's running it.
        ///   * If there is no entry for the language of the server, it will return an
        ///   empty string
        /// </summary>
        public static string GetModuleName()
        {
            return global::NWN.Core.NWScript.GetModuleName();
        }

        /// <summary>
        ///   Shut down the currently loaded module and start a new one (moving all
        ///   currently-connected players to the starting point.
        /// </summary>
        public static void StartNewModule(string sModuleName)
        {
            global::NWN.Core.NWScript.StartNewModule(sModuleName);
        }

        /// <summary>
        ///   Only if we are in a single player game, AutoSave the game.
        /// </summary>
        public static void DoSinglePlayerAutoSave()
        {
            global::NWN.Core.NWScript.DoSinglePlayerAutoSave();
        }

        /// <summary>
        ///   Get the game difficulty (GAME_DIFFICULTY_*).
        /// </summary>
        public static int GetGameDifficulty()
        {
            return global::NWN.Core.NWScript.GetGameDifficulty();
        }

        /// <summary>
        ///   End the currently running game, play sEndMovie then return all players to the
        ///   game's main menu.
        /// </summary>
        public static void EndGame(string sEndMovie)
        {
            global::NWN.Core.NWScript.EndGame(sEndMovie);
        }

        /// <summary>
        ///   Get the XP scale being used for the module.
        /// </summary>
        public static int GetModuleXPScale()
        {
            return global::NWN.Core.NWScript.GetModuleXPScale();
        }

        /// <summary>
        ///   Set the XP scale used by the module.
        ///   - nXPScale: The XP scale to be used. Must be between 0 and 200.
        /// </summary>
        public static void SetModuleXPScale(int nXPScale)
        {
            global::NWN.Core.NWScript.SetModuleXPScale(nXPScale);
        }

        /// <summary>
        ///   Write sLogEntry as a timestamped entry into the log file
        /// </summary>
        public static void WriteTimestampedLogEntry(string sLogEntry)
        {
            global::NWN.Core.NWScript.WriteTimestampedLogEntry(sLogEntry);
        }

        /// <summary>
        ///   Get the current action (ACTION_*) that oObject is executing.
        /// </summary>
        public static ActionType GetCurrentAction(uint oObject = OBJECT_INVALID)
        {
            return (ActionType)global::NWN.Core.NWScript.GetCurrentAction(oObject);
        }

        /// <summary>
        ///   Sets the active game pause state - same as if the player requested pause.
        /// </summary>
        public static void SetGameActivePause(bool bState)
        {
            global::NWN.Core.NWScript.SetGameActivePause(bState ? 1 : 0);
        }

        /// <summary>
        ///   Returns >0 if the game is currently paused:
        ///   - 0: Game is not paused.
        ///   - 1: Timestop
        ///   - 2: Active Player Pause (optionally on top of timestop)
        /// </summary>
        public static int GetGamePauseState()
        {
            return global::NWN.Core.NWScript.GetGamePauseState();
        }

        /// <summary>
        /// Return the current game tick rate (mainloop iterations per second).
        /// This is equivalent to graphics frames per second when the module is running inside a client.
        /// </summary>
        public static int GetTickRate()
        {
            return global::NWN.Core.NWScript.GetTickRate();
        }

        /// <summary>
        ///   Get the experience assigned in the journal editor for szPlotID.
        /// </summary>
        public static int GetJournalQuestExperience(string szPlotID)
        {
            return global::NWN.Core.NWScript.GetJournalQuestExperience(szPlotID);
        }

        /// <summary>
        /// Returns the 32bit integer hash of sString
        /// This hash is stable and will always have the same value for same input string, regardless of platform.
        /// The hash algorithm is the same as the one used internally for strings in case statements, so you can do:
        ///    switch (HashString(sString))
        ///    {
        ///         case "AAA":    HandleAAA(); break;
        ///         case "BBB":    HandleBBB(); break;
        ///    }
        /// NOTE: The exact algorithm used is XXH32(sString) ^ XXH32(""). This means that HashString("") is 0.
        /// </summary>
        public static int HashString(string sString)
        {
            return global::NWN.Core.NWScript.HashString(sString);
        }

        public static int GetMicrosecondCounter()
        {
            return global::NWN.Core.NWScript.GetMicrosecondCounter();
        }

        public static void ExecuteScript(string sScript, uint oTarget)
        {
            global::NWN.Core.NWScript.ExecuteScript(sScript, oTarget);
        }

        /// <summary>
        ///   Try to send oTarget to a new server defined by sIPaddress.
        ///   - oTarget
        ///   - sIPaddress: this can be numerical "192.168.0.84" or alphanumeric
        ///   "www.bioware.com". It can also contain a port "192.168.0.84:5121" or
        ///   "www.bioware.com:5121"; if the port is not specified, it will default to
        ///   5121.
        ///   - sPassword: login password for the destination server
        ///   - sWaypointTag: if this is set, after portalling the character will be moved
        ///   to this waypoint if it exists
        ///   - bSeamless: if this is set, the client wil not be prompted with the
        ///   information window telling them about the server, and they will not be
        ///   allowed to save a copy of their character if they are using a local vault
        ///   character.
        /// </summary>
        public static void ActivatePortal(uint oTarget, string sIPaddress = "", string sPassword = "",
            string sWaypointTag = "", bool bSeamless = false)
        {
            global::NWN.Core.NWScript.ActivatePortal(oTarget, sIPaddress, sPassword, sWaypointTag, bSeamless ? 1 : 0);
        }
    }
}
