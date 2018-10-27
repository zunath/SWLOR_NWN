using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXAdmin : NWNXBase, INWNXAdmin
    {
        public NWNXAdmin(INWScript script) 
            : base(script)
        {
        }


        /// <summary>
        /// Gets the current player password.
        /// </summary>
        /// <returns></returns>
        public string GetPlayerPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_PLAYER_PASSWORD");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Sets the current player password.
        /// </summary>
        /// <param name="password"></param>
        public void SetPlayerPassword(string password)
        {
            if (password == null) password = string.Empty;

            NWNX_PushArgumentString("NWNX_Administration", "SET_PLAYER_PASSWORD", password);
            NWNX_CallFunction("NWNX_Administration", "SET_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Removes the current player password.
        /// </summary>
        public void ClearPlayerPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "CLEAR_PLAYER_PASSWORD");
        }

        /// <summary>
        /// Gets the current DM password.
        /// </summary>
        /// <returns></returns>
        public string GetDMPassword()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_DM_PASSWORD");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_DM_PASSWORD");
        }

        /// <summary>
        /// Sets the current DM password. May be set to an empty string.
        /// </summary>
        /// <param name="password"></param>
        public void SetDMPassword(string password)
        {
            if (password == null) password = string.Empty;

            NWNX_PushArgumentString("NWNX_Administration", "SET_DM_PASSWORD", password);
            NWNX_CallFunction("NWNX_Administration", "SET_DM_PASSWORD");
        }

        /// <summary>
        /// Signals the server to immediately shut down.
        /// </summary>
        public void ShutdownServer()
        {
            NWNX_CallFunction("NWNX_Administration", "SHUTDOWN_SERVER");
        }

        /// <summary>
        /// Deletes the player character from the servervault
        /// The PC will be immediately booted from the game with a "Delete Character" message
        /// </summary>
        /// <param name="pc">The player character to boot.</param>
        /// <param name="bPreserveBackup">If true, it will leave the files on the server and append ".deleted0" to the bic file name.</param>
        public void DeletePlayerCharacter(NWPlayer pc, int bPreserveBackup)
        {
            NWNX_PushArgumentInt("NWNX_Administration", "DELETE_PLAYER_CHARACTER", bPreserveBackup);
            NWNX_PushArgumentObject("NWNX_Administration", "DELETE_PLAYER_CHARACTER", pc);
            NWNX_CallFunction("NWNX_Administration", "DELETE_PLAYER_CHARACTER");
        }

        /// <summary>
        /// Ban a given IP - get via GetPCIPAddress()
        /// </summary>
        /// <param name="ip"></param>
        public void AddBannedIP(string ip)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_IP", ip);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_IP");
        }

        /// <summary>
        /// Removes a banned IP address.
        /// </summary>
        /// <param name="ip"></param>
        public void RemoveBannedIP(string ip)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_IP", ip);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_IP");
        }

        /// <summary>
        /// Adds a banned CD key. Get via GetPCPublicCDKey
        /// </summary>
        /// <param name="key"></param>
        public void AddBannedCDKey(string key)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_CDKEY", key);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_CDKEY");
        }

        /// <summary>
        /// Removes a banned CD key.
        /// </summary>
        /// <param name="key"></param>
        public void RemoveBannedCDKey(string key)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_CDKEY", key);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_CDKEY");
        }

        /// <summary>
        /// Adds a banned player name - get via GetPCPlayerName.
        /// NOTE: Players can easily change their names.
        /// </summary>
        /// <param name="playername"></param>
        public void AddBannedPlayerName(string playername)
        {
            NWNX_PushArgumentString("NWNX_Administration", "ADD_BANNED_PLAYER_NAME", playername);
            NWNX_CallFunction("NWNX_Administration", "ADD_BANNED_PLAYER_NAME");
        }

        /// <summary>
        /// Removes a banned player name.
        /// </summary>
        /// <param name="playername"></param>
        public void RemoveBannedPlayerName(string playername)
        {
            NWNX_PushArgumentString("NWNX_Administration", "REMOVE_BANNED_PLAYER_NAME", playername);
            NWNX_CallFunction("NWNX_Administration", "REMOVE_BANNED_PLAYER_NAME");
        }

        /// <summary>
        /// Gets a list of all banned IPs, CD Keys, and player names as a string.
        /// </summary>
        /// <returns></returns>
        public string GetBannedList()
        {
            NWNX_CallFunction("NWNX_Administration", "GET_BANNED_LIST");
            return NWNX_GetReturnValueString("NWNX_Administration", "GET_BANNED_LIST");
        }


        /// <summary>
        /// Sets the module's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public void SetModuleName(string name)
        {
            NWNX_PushArgumentString("NWNX_Administration", "SET_MODULE_NAME", name);
            NWNX_CallFunction("NWNX_Administration", "SET_MODULE_NAME");
        }

        /// <summary>
        /// Sets the server's name as shown in the server list.
        /// </summary>
        /// <param name="name"></param>
        public void SetServerName(string name)
        {
            NWNX_PushArgumentString("NWNX_Administration", "SET_SERVER_NAME", name);
            NWNX_CallFunction("NWNX_Administration", "SET_SERVER_NAME");
        }

    }
}
