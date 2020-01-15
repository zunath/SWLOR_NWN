using System;
using NWN;
using SWLOR.Game.Server.NWScript;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXAdmin
    {
        private const string NWNX_Administration = "NWNX_Administration";

        /// <summary>
        /// Gets the current player password.
        /// </summary>
        /// <returns></returns>
        public static string GetPlayerPassword()
        {
            string sFunc = "GetPlayerPassword";

            NWNX_CallFunction(NWNX_Administration, sFunc);
            return NWNX_GetReturnValueString(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Sets the current player password.
        /// </summary>
        /// <param name="password"></param>
        public static void SetPlayerPassword(string password)
        {
            string sFunc = "SetPlayerPassword";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, password);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Removes the current player password.
        /// </summary>
        public static void ClearPlayerPassword()
        {
            string sFunc = "ClearPlayerPassword";

            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Gets the current DM password.
        /// </summary>
        /// <returns></returns>
        public static string GetDMPassword()
        {
            string sFunc = "GetDMPassword";

            NWNX_CallFunction(NWNX_Administration, sFunc);
            return NWNX_GetReturnValueString(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Sets the current DM password. May be set to an empty string.
        /// </summary>
        /// <param name="password"></param>
        public static void SetDMPassword(string password)
        {
            string sFunc = "SetDMPassword";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, password);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Signals the server to immediately shut down.
        /// </summary>
        public static void ShutdownServer()
        {
            string sFunc = "ShutdownServer";

            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Deletes the player character from the servervault
        /// The PC will be immediately booted from the game with a "Delete Character" message
        /// </summary>
        /// <param name="pc">The player character to boot.</param>
        /// <param name="bPreserveBackup">If true, it will leave the files on the server and append ".deleted0" to the bic file name.</param>
        public static void DeletePlayerCharacter(NWGameObject pc, bool bPreserveBackup)
        {
            string sFunc = "DeletePlayerCharacter";

            NWNX_PushArgumentInt(NWNX_Administration, sFunc, bPreserveBackup ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Administration, sFunc, pc);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Ban a given IP - get via GetPCIPAddress()
        /// </summary>
        /// <param name="ip"></param>
        public static void AddBannedIP(string ip)
        {
            string sFunc = "AddBannedIP";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, ip);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Removes a banned IP address.
        /// </summary>
        /// <param name="ip"></param>
        public static void RemoveBannedIP(string ip)
        {
            string sFunc = "RemoveBannedIP";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, ip);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Adds a banned CD key. Get via GetPCPublicCDKey
        /// </summary>
        /// <param name="key"></param>
        public static void AddBannedCDKey(string key)
        {
            string sFunc = "AddBannedCDKey";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, key);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Removes a banned CD key.
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveBannedCDKey(string key)
        {
            string sFunc = "RemoveBannedCDKey";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, key);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Adds a banned player name - get via GetPCPlayerName.
        /// NOTE: Players can easily change their names.
        /// </summary>
        /// <param name="playername"></param>
        public static void AddBannedPlayerName(string playerName)
        {
            string sFunc = "AddBannedPlayerName";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, playerName);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Removes a banned player name.
        /// </summary>
        /// <param name="playername"></param>
        public static void RemoveBannedPlayerName(string playerName)
        {
            string sFunc = "RemoveBannedPlayerName";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, playerName);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Gets a list of all banned IPs, CD Keys, and player names as a string.
        /// </summary>
        /// <returns></returns>
        public static string GetBannedList()
        {
            string sFunc = "GetBannedList";

            NWNX_CallFunction(NWNX_Administration, sFunc);
            return NWNX_GetReturnValueString(NWNX_Administration, sFunc);
        }


        /// <summary>
        /// Sets the module's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public static void SetModuleName(string name)
        {
            string sFunc = "SetModuleName";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, name);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Sets the server's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public static void SetServerName(string name)
        {
            string sFunc = "SetServerName";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, name);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Get an AdministrationOption value
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static int GetPlayOption(AdministrationOption option)
        {
            string sFunc = "GetPlayOption";

            NWNX_PushArgumentInt(NWNX_Administration, sFunc, (int)option);
            NWNX_CallFunction(NWNX_Administration, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Set an AdministrationOption value
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        public static void SetPlayOption(AdministrationOption option, int value)
        {
            string sFunc = "SetPlayOption";

            NWNX_PushArgumentInt(NWNX_Administration, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Administration, sFunc, (int)option);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }

        /// <summary>
        /// Delete the temporary user resource data (TURD) of a playerName + characterName
        /// </summary>
        /// <param name="playerName">Name of the player's user account</param>
        /// <param name="characterName">Name of the character</param>
        public static bool DeleteTURD(string playerName, string characterName)
        {
            string sFunc = "DeleteTURD";

            NWNX_PushArgumentString(NWNX_Administration, sFunc, characterName);
            NWNX_PushArgumentString(NWNX_Administration, sFunc, playerName);
            NWNX_CallFunction(NWNX_Administration, sFunc);

            return Convert.ToBoolean(NWNX_GetReturnValueInt(NWNX_Administration, sFunc));
        }

        /// <summary>
        /// Get an admin_debug "Administration Debug Type" value.
        /// </summary>
        /// <param name="type">An "Administration Debug Type"</param>
        /// <returns>The current value for the supplied debug type from admin_debug "Administration Debug Types".</returns>
        public static bool GetDebugValue(AdministrationDebugType type)
        {
            string sFunc = "GetDebugValue";

            NWNX_PushArgumentInt(NWNX_Administration, sFunc, (int)type);
            NWNX_CallFunction(NWNX_Administration, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Administration, sFunc) == 1;
        }

        /// <summary>
        /// Set an "Administration Debug Type" to a value.
        /// </summary>
        /// <param name="type">The debug type to adjust from "Administration Debug Types".</param>
        /// <param name="state">The new state for the debug type, true or false</param>
        public static void SetDebugValue(AdministrationDebugType type, bool state)
        {
            string sFunc = "SetDebugValue";

            NWNX_PushArgumentInt(NWNX_Administration, sFunc, state ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_Administration, sFunc, (int)type);
            NWNX_CallFunction(NWNX_Administration, sFunc);
        }
    }
}
