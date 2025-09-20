using NWN.Core.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides administrative functions for server management, player management, and server configuration.
    /// This plugin handles password management, player banning, server settings, and debug options.
    /// </summary>
    public static class AdministrationPlugin
    {
        /// <summary>
        /// Gets the current player password used for server access.
        /// </summary>
        /// <returns>The current player password as a string. Returns empty string if no password is set.</returns>
        /// <remarks>
        /// This password is required for players to connect to the server.
        /// Use SetPlayerPassword() to change it or ClearPlayerPassword() to remove it.
        /// </remarks>
        public static string GetPlayerPassword()
        {
            return AdminPlugin.GetPlayerPassword();
        }

        /// <summary>
        /// Sets the player password required for server access.
        /// </summary>
        /// <param name="password">The new password to set for player access. Cannot be null.</param>
        /// <remarks>
        /// All players will need to provide this password when connecting to the server.
        /// Set to empty string to effectively disable password protection.
        /// </remarks>
        public static void SetPlayerPassword(string password)
        {
            AdminPlugin.SetPlayerPassword(password);
        }

        /// <summary>
        /// Removes the current player password, allowing unrestricted access to the server.
        /// </summary>
        /// <remarks>
        /// After calling this method, players will no longer need a password to connect.
        /// This is equivalent to setting an empty password.
        /// </remarks>
        public static void ClearPlayerPassword()
        {
            AdminPlugin.ClearPlayerPassword();
        }

        /// <summary>
        /// Gets the current DM password used for administrative access.
        /// </summary>
        /// <returns>The current DM password as a string. Returns empty string if no password is set.</returns>
        /// <remarks>
        /// This password is required for players to gain DM privileges on the server.
        /// Use SetDMPassword() to change it.
        /// </remarks>
        public static string GetDMPassword()
        {
            return AdminPlugin.GetDMPassword();
        }

        /// <summary>
        /// Sets the DM password required for administrative access.
        /// </summary>
        /// <param name="password">The new password to set for DM access. Can be empty string to disable DM password.</param>
        /// <remarks>
        /// Players must provide this password to gain DM privileges on the server.
        /// Setting to empty string effectively disables DM password protection.
        /// </remarks>
        public static void SetDMPassword(string password)
        {
            AdminPlugin.SetDMPassword(password);
        }

        /// <summary>
        /// Immediately shuts down the server and disconnects all players.
        /// </summary>
        /// <remarks>
        /// This is an immediate shutdown with no grace period. All players will be disconnected
        /// and the server process will terminate. Use with caution in production environments.
        /// </remarks>
        public static void ShutdownServer()
        {
            AdminPlugin.ShutdownServer();
        }

        /// <summary>
        /// Permanently deletes a player character from the server vault.
        /// </summary>
        /// <param name="pc">The player character object to delete. Must be a valid player character.</param>
        /// <param name="bPreserveBackup">If true, creates a backup of the character file before deletion. Default is true.</param>
        /// <param name="kickMessage">Optional custom message to display to the player when they are disconnected. Default is empty string.</param>
        /// <remarks>
        /// This action is irreversible and will immediately disconnect the player from the server.
        /// The character data will be permanently removed from the server vault.
        /// If bPreserveBackup is true, a backup file will be created in the server vault backup directory.
        /// </remarks>
        public static void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "")
        {
            AdminPlugin.DeletePlayerCharacter(pc, bPreserveBackup ? 1 : 0, kickMessage);
        }

        /// <summary>
        /// Adds an IP address to the server's ban list, preventing connections from that IP.
        /// </summary>
        /// <param name="ip">The IP address to ban. Use GetPCIPAddress() to obtain a player's IP address.</param>
        /// <remarks>
        /// Banned IP addresses are stored in the server's ban list and checked during connection attempts.
        /// The ban takes effect immediately for new connection attempts.
        /// Use GetBannedList() to view all banned IPs or RemoveBannedIP() to remove a ban.
        /// </remarks>
        public static void AddBannedIP(string ip)
        {
            AdminPlugin.AddBannedIP(ip);
        }

        /// <summary>
        /// Removes an IP address from the server's ban list, allowing connections from that IP.
        /// </summary>
        /// <param name="ip">The IP address to unban. Must match exactly with a previously banned IP.</param>
        /// <remarks>
        /// This allows the previously banned IP address to connect to the server again.
        /// The change takes effect immediately for new connection attempts.
        /// </remarks>
        public static void RemoveBannedIP(string ip)
        {
            AdminPlugin.RemoveBannedIP(ip);
        }

        /// <summary>
        /// Adds a CD key to the server's ban list, preventing players with that CD key from connecting.
        /// </summary>
        /// <param name="key">The CD key to ban. Use GetPCPublicCDKey() to obtain a player's CD key.</param>
        /// <remarks>
        /// Banned CD keys are stored in the server's ban list and checked during connection attempts.
        /// This prevents specific players from connecting regardless of their IP address.
        /// The ban takes effect immediately for new connection attempts.
        /// </remarks>
        public static void AddBannedCDKey(string key)
        {
            AdminPlugin.AddBannedCDKey(key);
        }

        /// <summary>
        /// Removes a CD key from the server's ban list, allowing players with that CD key to connect.
        /// </summary>
        /// <param name="key">The CD key to unban. Must match exactly with a previously banned CD key.</param>
        /// <remarks>
        /// This allows the previously banned CD key to connect to the server again.
        /// The change takes effect immediately for new connection attempts.
        /// </remarks>
        public static void RemoveBannedCDKey(string key)
        {
            AdminPlugin.RemoveBannedCDKey(key);
        }

        /// <summary>
        /// Adds a player name to the server's ban list, preventing players with that name from connecting.
        /// </summary>
        /// <param name="playerName">The player name to ban. Use GetPCPlayerName() to obtain a player's name.</param>
        /// <remarks>
        /// Banned player names are stored in the server's ban list and checked during character creation and login.
        /// This prevents specific player names from being used on the server.
        /// The ban takes effect immediately for new character creation attempts.
        /// </remarks>
        public static void AddBannedPlayerName(string playerName)
        {
            AdminPlugin.AddBannedPlayerName(playerName);
        }

        /// <summary>
        /// Removes a player name from the server's ban list, allowing that name to be used again.
        /// </summary>
        /// <param name="playerName">The player name to unban. Must match exactly with a previously banned name.</param>
        /// <remarks>
        /// This allows the previously banned player name to be used for character creation again.
        /// The change takes effect immediately for new character creation attempts.
        /// </remarks>
        public static void RemoveBannedPlayerName(string playerName)
        {
            AdminPlugin.RemoveBannedPlayerName(playerName);
        }

        /// <summary>
        /// Retrieves a formatted list of all banned IP addresses, CD keys, and player names.
        /// </summary>
        /// <returns>A string containing all banned items, formatted for display or logging purposes.</returns>
        /// <remarks>
        /// The returned string includes all three types of bans: IP addresses, CD keys, and player names.
        /// This is useful for administrative purposes, logging, or displaying ban information to administrators.
        /// </remarks>
        public static string GetBannedList()
        {
            return AdminPlugin.GetBannedList();
        }

        /// <summary>
        /// Sets the module's display name as shown in the server browser and server list.
        /// </summary>
        /// <param name="name">The new name to display for the module. Cannot be null or empty.</param>
        /// <remarks>
        /// This name is visible to players when browsing available servers.
        /// The change takes effect immediately and is persistent across server restarts.
        /// </remarks>
        public static void SetModuleName(string name)
        {
            AdminPlugin.SetModuleName(name);
        }

        /// <summary>
        /// Sets the server's display name as shown in the server browser and server list.
        /// </summary>
        /// <param name="name">The new name to display for the server. Cannot be null or empty.</param>
        /// <remarks>
        /// This name is visible to players when browsing available servers.
        /// The change takes effect immediately and is persistent across server restarts.
        /// </remarks>
        public static void SetServerName(string name)
        {
            AdminPlugin.SetServerName(name);
        }

        /// <summary>
        /// Retrieves the current value of a specific administration option.
        /// </summary>
        /// <param name="option">The administration option to query. See AdministrationOption enum for available options.</param>
        /// <returns>True if the option is enabled, false if disabled.</returns>
        /// <remarks>
        /// Administration options control various server behaviors and policies.
        /// Use SetPlayOption() to modify these values.
        /// </remarks>
        public static bool GetPlayOption(AdministrationOption option)
        {
            int result = AdminPlugin.GetPlayOption((int)option);
            return result != 0;
        }

        /// <summary>
        /// Sets the value of a specific administration option.
        /// </summary>
        /// <param name="option">The administration option to modify. See AdministrationOption enum for available options.</param>
        /// <param name="value">True to enable the option, false to disable it.</param>
        /// <remarks>
        /// Administration options control various server behaviors and policies.
        /// Changes take effect immediately and may affect all connected players.
        /// Use GetPlayOption() to query current values.
        /// </remarks>
        public static void SetPlayOption(AdministrationOption option, bool value)
        {
            AdminPlugin.SetPlayOption((int)option, value ? 1 : 0);
        }

        /// <summary>
        /// Deletes the Temporary User Resource Data (TURD) for a specific player and character combination.
        /// </summary>
        /// <param name="playerName">The player's account name. Must match exactly with the server's records.</param>
        /// <param name="characterName">The character's name. Must match exactly with the server's records.</param>
        /// <returns>True if the TURD was successfully deleted, false if the TURD was not found or deletion failed.</returns>
        /// <remarks>
        /// TURD files contain temporary character data that persists between sessions.
        /// Deleting a TURD will cause the character to lose any unsaved progress or temporary data.
        /// This is useful for cleaning up corrupted or problematic character data.
        /// </remarks>
        public static bool DeleteTURD(string playerName, string characterName)
        {
            int result = AdminPlugin.DeleteTURD(playerName, characterName);
            return result != 0;
        }

        /// <summary>
        /// Retrieves the current state of a specific administration debug option.
        /// </summary>
        /// <param name="type">The debug type to query. See AdministrationDebugType enum for available types.</param>
        /// <returns>True if the debug option is enabled, false if disabled.</returns>
        /// <remarks>
        /// Debug options provide additional logging and diagnostic information for server administration.
        /// These options can help troubleshoot issues but may impact server performance when enabled.
        /// Use SetDebugValue() to modify these settings.
        /// </remarks>
        public static bool GetDebugValue(AdministrationDebugType type)
        {
            int result = AdminPlugin.GetDebugValue((int)type);
            return result != 0;
        }

        /// <summary>
        /// Sets the state of a specific administration debug option.
        /// </summary>
        /// <param name="type">The debug type to modify. See AdministrationDebugType enum for available types.</param>
        /// <param name="state">True to enable debug logging for this type, false to disable it.</param>
        /// <remarks>
        /// Debug options provide additional logging and diagnostic information for server administration.
        /// Enabling debug options may impact server performance due to increased logging.
        /// Changes take effect immediately and affect all debug output of the specified type.
        /// </remarks>
        public static void SetDebugValue(AdministrationDebugType type, bool state)
        {
            AdminPlugin.SetDebugValue((int)type, state ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the server's current minimum character level setting.
        /// </summary>
        /// <returns>The minimum character level allowed on the server.</returns>
        /// <remarks>
        /// Characters below this level will not be able to connect to the server.
        /// This setting helps enforce server policies and can be used to restrict access to higher-level content.
        /// Use SetMinLevel() to modify this value.
        /// </remarks>
        public static int GetMinLevel()
        {
            return AdminPlugin.GetMinLevel();
        }

        /// <summary>
        /// Sets the server's minimum character level requirement.
        /// </summary>
        /// <param name="nLevel">The minimum character level required to connect to the server. Must be a positive integer.</param>
        /// <remarks>
        /// Characters below this level will not be able to connect to the server.
        /// This setting helps enforce server policies and can be used to restrict access to higher-level content.
        /// The change takes effect immediately for new connection attempts.
        /// </remarks>
        public static void SetMinLevel(int nLevel)
        {
            AdminPlugin.SetMinLevel(nLevel);
        }

        /// <summary>
        /// Retrieves the server's current maximum character level setting.
        /// </summary>
        /// <returns>The maximum character level allowed on the server.</returns>
        /// <remarks>
        /// Characters cannot exceed this level through normal gameplay.
        /// This setting helps maintain game balance and server performance.
        /// Use SetMaxLevel() to modify this value.
        /// Note: This conflicts with the MaxLevel plugin - they both control the same server setting.
        /// </remarks>
        public static int GetMaxLevel()
        {
            return AdminPlugin.GetMaxLevel();
        }

        /// <summary>
        /// Sets the server's maximum character level limit.
        /// </summary>
        /// <param name="nLevel">The maximum character level allowed on the server. Must be a positive integer.</param>
        /// <remarks>
        /// Characters cannot exceed this level through normal gameplay.
        /// This setting helps maintain game balance and server performance.
        /// The change takes effect immediately and affects all character advancement.
        /// Warning: This conflicts with the MaxLevel plugin - they both control the same server setting.
        /// Use only one method to control maximum level to avoid conflicts.
        /// </remarks>
        public static void SetMaxLevel(int nLevel)
        {
            AdminPlugin.SetMaxLevel(nLevel);
        }

        /// <summary>
        /// Retrieves the current server name as displayed in the server browser.
        /// </summary>
        /// <returns>The current server name as a string.</returns>
        /// <remarks>
        /// This returns the server name that is visible to players when browsing available servers.
        /// Use SetServerName() to modify this value.
        /// </remarks>
        public static string GetServerName()
        {
            return AdminPlugin.GetServerName();
        }

        /// <summary>
        /// Reloads the server rules from the configuration files.
        /// </summary>
        /// <remarks>
        /// This causes the server to re-read its configuration files and apply any changes.
        /// Useful for applying configuration changes without restarting the server.
        /// The reload affects all server settings and rules.
        /// </remarks>
        public static void ReloadRules()
        {
            AdminPlugin.ReloadRules();
        }
    }
}