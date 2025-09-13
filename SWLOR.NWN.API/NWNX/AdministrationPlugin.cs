using NWN.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class AdministrationPlugin
    {
        /// <summary>
        /// Gets the current player password.
        /// </summary>
        /// <returns>The current player password.</returns>
        public static string GetPlayerPassword()
        {
            return AdminPlugin.GetPlayerPassword();
        }

        /// <summary>
        /// Sets the current player password.
        /// </summary>
        /// <param name="password">The password to set.</param>
        public static void SetPlayerPassword(string password)
        {
            AdminPlugin.SetPlayerPassword(password);
        }

        /// <summary>
        /// Removes the current player password.
        /// </summary>
        public static void ClearPlayerPassword()
        {
            AdminPlugin.ClearPlayerPassword();
        }

        /// <summary>
        /// Gets the current DM password.
        /// </summary>
        /// <returns>The current DM password.</returns>
        public static string GetDMPassword()
        {
            return AdminPlugin.GetDMPassword();
        }

        /// <summary>
        /// Sets the current DM password. May be set to an empty string.
        /// </summary>
        /// <param name="password">The password to set.</param>
        public static void SetDMPassword(string password)
        {
            AdminPlugin.SetDMPassword(password);
        }

        /// <summary>
        /// Signals the server to immediately shut down.
        /// </summary>
        public static void ShutdownServer()
        {
            AdminPlugin.ShutdownServer();
        }

        /// <summary>
        /// Deletes the player character from the servervault.
        /// The PC will be immediately booted from the game with a "Delete Character" message.
        /// </summary>
        /// <param name="pc">The player character to delete.</param>
        /// <param name="bPreserveBackup">If true, preserves a backup of the character file.</param>
        /// <param name="kickMessage">Optional kick message to display to the player.</param>
        public static void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "")
        {
            AdminPlugin.DeletePlayerCharacter(pc, bPreserveBackup ? 1 : 0, kickMessage);
        }

        /// <summary>
        /// Bans a given IP address.
        /// </summary>
        /// <param name="ip">The IP address to ban. Get via GetPCIPAddress().</param>
        public static void AddBannedIP(string ip)
        {
            AdminPlugin.AddBannedIP(ip);
        }

        /// <summary>
        /// Removes a banned IP address.
        /// </summary>
        /// <param name="ip">The IP address to unban.</param>
        public static void RemoveBannedIP(string ip)
        {
            AdminPlugin.RemoveBannedIP(ip);
        }

        /// <summary>
        /// Adds a banned CD key.
        /// </summary>
        /// <param name="key">The CD key to ban. Get via GetPCPublicCDKey.</param>
        public static void AddBannedCDKey(string key)
        {
            AdminPlugin.AddBannedCDKey(key);
        }

        /// <summary>
        /// Removes a banned CD key.
        /// </summary>
        /// <param name="key">The CD key to unban.</param>
        public static void RemoveBannedCDKey(string key)
        {
            AdminPlugin.RemoveBannedCDKey(key);
        }

        /// <summary>
        /// Adds a banned player name.
        /// </summary>
        /// <param name="playerName">The player name to ban. Get via GetPCPlayerName.</param>
        public static void AddBannedPlayerName(string playerName)
        {
            AdminPlugin.AddBannedPlayerName(playerName);
        }

        /// <summary>
        /// Removes a banned player name.
        /// </summary>
        /// <param name="playerName">The player name to unban.</param>
        public static void RemoveBannedPlayerName(string playerName)
        {
            AdminPlugin.RemoveBannedPlayerName(playerName);
        }

        /// <summary>
        /// Gets a list of all banned IPs, CD Keys, and player names as a string.
        /// </summary>
        /// <returns>A string containing all banned items.</returns>
        public static string GetBannedList()
        {
            return AdminPlugin.GetBannedList();
        }

        /// <summary>
        /// Sets the module's name as shown in the server list.
        /// </summary>
        /// <param name="name">The name to set for the module.</param>
        public static void SetModuleName(string name)
        {
            AdminPlugin.SetModuleName(name);
        }

        /// <summary>
        /// Sets the server's name as shown in the server list.
        /// </summary>
        /// <param name="name">The name to set for the server.</param>
        public static void SetServerName(string name)
        {
            AdminPlugin.SetServerName(name);
        }

        /// <summary>
        /// Gets an AdministrationOption value.
        /// </summary>
        /// <param name="option">The administration option to get.</param>
        /// <returns>The current value of the option.</returns>
        public static bool GetPlayOption(AdministrationOption option)
        {
            int result = AdminPlugin.GetPlayOption((int)option);
            return result != 0;
        }

        /// <summary>
        /// Sets an AdministrationOption value.
        /// </summary>
        /// <param name="option">The administration option to set.</param>
        /// <param name="value">The value to set for the option.</param>
        public static void SetPlayOption(AdministrationOption option, bool value)
        {
            AdminPlugin.SetPlayOption((int)option, value ? 1 : 0);
        }

        /// <summary>
        /// Deletes the temporary user resource data (TURD) of a playerName + characterName.
        /// </summary>
        /// <param name="playerName">The player name.</param>
        /// <param name="characterName">The character name.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool DeleteTURD(string playerName, string characterName)
        {
            int result = AdminPlugin.DeleteTURD(playerName, characterName);
            return result != 0;
        }

        /// <summary>
        /// Gets an admin_debug "Administration Debug Type" value.
        /// </summary>
        /// <param name="type">The administration debug type.</param>
        /// <returns>The current value for the supplied debug type.</returns>
        public static bool GetDebugValue(AdministrationDebugType type)
        {
            int result = AdminPlugin.GetDebugValue((int)type);
            return result != 0;
        }

        /// <summary>
        /// Sets an "Administration Debug Type" to a value.
        /// </summary>
        /// <param name="type">The debug type to adjust from "Administration Debug Types".</param>
        /// <param name="state">The new state for the debug type, true or false.</param>
        public static void SetDebugValue(AdministrationDebugType type, bool state)
        {
            AdminPlugin.SetDebugValue((int)type, state ? 1 : 0);
        }

        /// <summary>
        /// Gets the server's minimum level.
        /// </summary>
        /// <returns>The minimum level for the server.</returns>
        public static int GetMinLevel()
        {
            return AdminPlugin.GetMinLevel();
        }

        /// <summary>
        /// Sets the server's minimum level.
        /// </summary>
        /// <param name="nLevel">The minimum level for the server.</param>
        public static void SetMinLevel(int nLevel)
        {
            AdminPlugin.SetMinLevel(nLevel);
        }

        /// <summary>
        /// Gets the server's maximum level.
        /// </summary>
        /// <returns>The maximum level for the server.</returns>
        public static int GetMaxLevel()
        {
            return AdminPlugin.GetMaxLevel();
        }

        /// <summary>
        /// Sets the server's maximum level.
        /// </summary>
        /// <param name="nLevel">The maximum level for the server.</param>
        /// <remarks>Attention when using this and the MaxLevel plugin. They both change the same value.</remarks>
        public static void SetMaxLevel(int nLevel)
        {
            AdminPlugin.SetMaxLevel(nLevel);
        }
    }
}