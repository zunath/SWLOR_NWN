using SWLOR.Game.Server.GameObject;

using static NWN._;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXAdmin
    {
        /// <summary>
        /// Gets the current player password.
        /// </summary>
        /// <returns></returns>
        public static string GetPlayerPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_PLAYER_PASSWORD");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Sets the current player password.
        /// </summary>
        /// <param name="password"></param>
        public static void SetPlayerPassword(string password)
        {
            if (password == null) password = string.Empty;

            NWNX_PushArgumentString("NWNX_Administration", "SET_PLAYER_PASSWORD", password);
            NWNX_CallFunction("NWNX_Administration", "SET_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Removes the current player password.
        /// </summary>
        public static void ClearPlayerPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "CLEAR_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Gets the current DM password.
        /// </summary>
        /// <returns></returns>
        public static string GetDMPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_DM_PASSWORD");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_DM_PASSWORD");
        }

        /// <summary>
        /// Sets the current DM password. May be set to an empty string.
        /// </summary>
        /// <param name="password"></param>
        public static void SetDMPassword(string password)
        {
            if (password == null) password = string.Empty;

            NWNX_PushArgumentString("NWNX_Administration", "SET_DM_PASSWORD", password);
            NWNX_CallFunction("NWNX_Administration", "SET_DM_PASSWORD");
        }

        /// <summary>
        /// Signals the server to immediately shut down.
        /// </summary>
        public static void ShutdownServer()
        {
            NWNX_CallFunction("NWNX_Administration", "SHUTDOWN_SERVER");
        }

        /// <summary>
        /// Deletes the player character from the servervault
        /// The PC will be immediately booted from the game with a "Delete Character" message
        /// </summary>
        /// <param name="pc">The player character to boot.</param>
        /// <param name="bPreserveBackup">If true, it will leave the files on the server and append ".deleted0" to the bic file name.</param>
        public static void DeletePlayerCharacter(NWPlayer pc, bool bPreserveBackup)
        {
            NWNX_PushArgumentInt("NWNX_Administration", "DELETE_PLAYER_CHARACTER", bPreserveBackup ? TRUE : FALSE);
            NWNX_PushArgumentObject("NWNX_Administration", "DELETE_PLAYER_CHARACTER", pc);
            NWNX_CallFunction("NWNX_Administration", "DELETE_PLAYER_CHARACTER");
        }

        /// <summary>
        /// Ban a given IP - get via GetPCIPAddress()
        /// </summary>
        /// <param name="ip"></param>
        public static void AddBannedIP(string ip)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_IP", ip);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_IP");
        }

        /// <summary>
        /// Removes a banned IP address.
        /// </summary>
        /// <param name="ip"></param>
        public static void RemoveBannedIP(string ip)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_IP", ip);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_IP");
        }

        /// <summary>
        /// Adds a banned CD key. Get via GetPCPublicCDKey
        /// </summary>
        /// <param name="key"></param>
        public static void AddBannedCDKey(string key)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_CDKEY", key);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_CDKEY");
        }

        /// <summary>
        /// Removes a banned CD key.
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveBannedCDKey(string key)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_CDKEY", key);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_CDKEY");
        }

        /// <summary>
        /// Adds a banned player name - get via GetPCPlayerName.
        /// NOTE: Players can easily change their names.
        /// </summary>
        /// <param name="playername"></param>
        public static void AddBannedPlayerName(string playername)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_PLAYER_NAME", playername);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_PLAYER_NAME");
        }

        /// <summary>
        /// Removes a banned player name.
        /// </summary>
        /// <param name="playername"></param>
        public static void RemoveBannedPlayerName(string playername)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_PLAYER_NAME", playername);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_PLAYER_NAME");
        }

        /// <summary>
        /// Gets a list of all banned IPs, CD Keys, and player names as a string.
        /// </summary>
        /// <returns></returns>
        public static string GetBannedList()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_BANNED_LIST");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_BANNED_LIST");
        }


        /// <summary>
        /// Sets the module's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public static void SetModuleName(string name)
        {
            NWNX_PushArgumentString("NWNX_Administration", "SET_MODULE_NAME", name);
            NWNX_CallFunction("NWNX_Administration", "SET_MODULE_NAME");
        }

        /// <summary>
        /// Sets the server's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public static void SetServerName(string name)
        {
            NWNX_PushArgumentString("NWNX_Administration", "SET_SERVER_NAME", name);
            NWNX_CallFunction("NWNX_Administration", "SET_SERVER_NAME");
        }

        /// <summary>
        /// Get an AdministrationOption value
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static int GetPlayOption(AdministrationOption option)
        {
            NWNX_PushArgumentInt("NWNX_Administration", "GET_PLAY_OPTION", (int)option);
            NWNX_CallFunction("NWNX_Administration", "GET_PLAY_OPTION");

            return NWNX_GetReturnValueInt("NWNX_Administration", "GET_PLAY_OPTION");
        }

        /// <summary>
        /// Set an AdministrationOption value
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        public static void SetPlayOption(AdministrationOption option, int value)
        {
            NWNX_PushArgumentInt("NWNX_Administration", "SET_PLAY_OPTION", value);
            NWNX_PushArgumentInt("NWNX_Administration", "SET_PLAY_OPTION", (int)option);
            NWNX_CallFunction("NWNX_Administration", "SET_PLAY_OPTION");
        }

        /// <summary>
        /// Delete the temporary user resource data (TURD) of a playerName + characterName
        /// </summary>
        /// <param name="playerName">Name of the player's user account</param>
        /// <param name="characterName">Name of the character</param>
        public static void DeleteTURD(string playerName, string characterName)
        {
            NWNX_PushArgumentString("NWNX_Administration", "DELETE_TURD", characterName);
            NWNX_PushArgumentString("NWNX_Administration", "DELETE_TURD", playerName);
            NWNX_CallFunction("NWNX_Administration", "DELETE_TURD");
        }
    }
}
