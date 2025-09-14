using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the starting location of the module.
        /// </summary>
        /// <returns>The starting location of the module</returns>
        public static Location GetStartingLocation()
        {
            return global::NWN.Core.NWScript.GetStartingLocation();
        }

        /// <summary>
        /// Gets the module's name in the language of the server that's running it.
        /// </summary>
        /// <returns>The module's name. Returns an empty string if there is no entry for the language of the server</returns>
        public static string GetModuleName()
        {
            return global::NWN.Core.NWScript.GetModuleName();
        }

        /// <summary>
        /// Shuts down the currently loaded module and starts a new one.
        /// </summary>
        /// <param name="sModuleName">The name of the new module to start</param>
        /// <remarks>Moves all currently-connected players to the starting point.</remarks>
        public static void StartNewModule(string sModuleName)
        {
            global::NWN.Core.NWScript.StartNewModule(sModuleName);
        }

        /// <summary>
        /// Auto-saves the game if we are in a single player game.
        /// </summary>
        /// <remarks>Only works in single player games.</remarks>
        public static void DoSinglePlayerAutoSave()
        {
            global::NWN.Core.NWScript.DoSinglePlayerAutoSave();
        }

        /// <summary>
        /// Gets the game difficulty.
        /// </summary>
        /// <returns>The game difficulty (GAME_DIFFICULTY_* constants)</returns>
        public static int GetGameDifficulty()
        {
            return global::NWN.Core.NWScript.GetGameDifficulty();
        }

        /// <summary>
        /// Ends the currently running game and returns all players to the main menu.
        /// </summary>
        /// <param name="sEndMovie">The movie to play before ending the game</param>
        public static void EndGame(string sEndMovie)
        {
            global::NWN.Core.NWScript.EndGame(sEndMovie);
        }

        /// <summary>
        /// Gets the XP scale being used for the module.
        /// </summary>
        /// <returns>The XP scale being used for the module</returns>
        public static int GetModuleXPScale()
        {
            return global::NWN.Core.NWScript.GetModuleXPScale();
        }

        /// <summary>
        /// Sets the XP scale used by the module.
        /// </summary>
        /// <param name="nXPScale">The XP scale to be used (must be between 0 and 200)</param>
        public static void SetModuleXPScale(int nXPScale)
        {
            global::NWN.Core.NWScript.SetModuleXPScale(nXPScale);
        }

        /// <summary>
        /// Writes a timestamped entry into the log file.
        /// </summary>
        /// <param name="sLogEntry">The log entry to write</param>
        public static void WriteTimestampedLogEntry(string sLogEntry)
        {
            global::NWN.Core.NWScript.WriteTimestampedLogEntry(sLogEntry);
        }

        /// <summary>
        /// Gets the current action that the specified object is executing.
        /// </summary>
        /// <param name="oObject">The object to get the current action for (default: OBJECT_SELF)</param>
        /// <returns>The current action (ACTION_* constants)</returns>
        public static ActionType GetCurrentAction(uint oObject = OBJECT_INVALID)
        {
            if (oObject == OBJECT_INVALID)
                oObject = OBJECT_SELF;
            return (ActionType)global::NWN.Core.NWScript.GetCurrentAction(oObject);
        }

        /// <summary>
        /// Sets the active game pause state.
        /// </summary>
        /// <param name="bState">The pause state to set</param>
        /// <remarks>Same as if the player requested pause.</remarks>
        public static void SetGameActivePause(bool bState)
        {
            global::NWN.Core.NWScript.SetGameActivePause(bState ? 1 : 0);
        }

        /// <summary>
        /// Returns a value greater than 0 if the game is currently paused.
        /// </summary>
        /// <returns>0: Game is not paused, 1: Timestop, 2: Active Player Pause (optionally on top of timestop)</returns>
        public static int GetGamePauseState()
        {
            return global::NWN.Core.NWScript.GetGamePauseState();
        }

        /// <summary>
        /// Returns the current game tick rate.
        /// </summary>
        /// <returns>The current game tick rate (mainloop iterations per second)</returns>
        /// <remarks>This is equivalent to graphics frames per second when the module is running inside a client.</remarks>
        public static int GetTickRate()
        {
            return global::NWN.Core.NWScript.GetTickRate();
        }

        /// <summary>
        /// Gets the experience assigned in the journal editor for the specified plot ID.
        /// </summary>
        /// <param name="szPlotID">The plot ID to get the experience for</param>
        /// <returns>The experience assigned for the plot ID</returns>
        public static int GetJournalQuestExperience(string szPlotID)
        {
            return global::NWN.Core.NWScript.GetJournalQuestExperience(szPlotID);
        }

        /// <summary>
        /// Returns the 32-bit integer hash of the specified string.
        /// </summary>
        /// <param name="sString">The string to hash</param>
        /// <returns>The 32-bit integer hash of the string</returns>
        /// <remarks>This hash is stable and will always have the same value for same input string, regardless of platform. The hash algorithm is the same as the one used internally for strings in case statements, so you can do: switch (HashString(sString)) { case "AAA": HandleAAA(); break; case "BBB": HandleBBB(); break; } The exact algorithm used is XXH32(sString) ^ XXH32(""). This means that HashString("") is 0.</remarks>
        public static int HashString(string sString)
        {
            return global::NWN.Core.NWScript.HashString(sString);
        }

        public static int GetMicrosecondCounter()
        {
            return global::NWN.Core.NWScript.GetMicrosecondCounter();
        }

        public static void ExecuteScript(string sScript, uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
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
