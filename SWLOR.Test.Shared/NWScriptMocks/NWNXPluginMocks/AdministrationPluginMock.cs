using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the AdministrationPlugin for testing purposes.
    /// Provides comprehensive administrative functions for server management, player management, and server configuration.
    /// </summary>
    public class AdministrationPluginMock: IAdministrationPluginService
    {
        // Mock data storage
        private string _playerPassword = "";
        private string _dmPassword = "";
        private string _moduleName = "Test Module";
        private string _serverName = "Test Server";
        private int _minLevel = 1;
        private int _maxLevel = 40;
        private readonly Dictionary<AdministrationOptionType, bool> _playOptions = new();
        private readonly Dictionary<AdministrationDebugType, bool> _debugValues = new();
        private readonly List<string> _bannedIPs = new();
        private readonly List<string> _bannedCDKeys = new();
        private readonly List<string> _bannedPlayerNames = new();

        /// <summary>
        /// Gets the current player password used for server access.
        /// </summary>
        /// <returns>The current player password as a string. Returns empty string if no password is set.</returns>
        public string GetPlayerPassword()
        {
            return _playerPassword;
        }

        /// <summary>
        /// Sets the player password required for server access.
        /// </summary>
        /// <param name="password">The new password to set for player access. Cannot be null.</param>
        public void SetPlayerPassword(string password)
        {
            _playerPassword = password ?? "";
        }

        /// <summary>
        /// Removes the current player password, allowing unrestricted access to the server.
        /// </summary>
        public void ClearPlayerPassword()
        {
            _playerPassword = "";
        }

        /// <summary>
        /// Gets the current DM password used for administrative access.
        /// </summary>
        /// <returns>The current DM password as a string. Returns empty string if no password is set.</returns>
        public string GetDMPassword()
        {
            return _dmPassword;
        }

        /// <summary>
        /// Sets the DM password required for administrative access.
        /// </summary>
        /// <param name="password">The new password to set for DM access. Can be empty string to disable DM password.</param>
        public void SetDMPassword(string password)
        {
            _dmPassword = password ?? "";
        }

        /// <summary>
        /// Immediately shuts down the server and disconnects all players.
        /// </summary>
        public void ShutdownServer()
        {
            // Mock implementation - in real tests, this would trigger shutdown
        }

        /// <summary>
        /// Permanently deletes a player character from the server vault.
        /// </summary>
        /// <param name="pc">The player character object to delete. Must be a valid player character.</param>
        /// <param name="bPreserveBackup">If true, creates a backup of the character file before deletion. Default is true.</param>
        /// <param name="kickMessage">Optional custom message to display to the player when they are disconnected. Default is empty string.</param>
        public void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "")
        {
            // Mock implementation - in real tests, this would delete the character
        }

        /// <summary>
        /// Adds an IP address to the server's ban list, preventing connections from that IP.
        /// </summary>
        /// <param name="ip">The IP address to ban. Use GetPCIPAddress() to obtain a player's IP address.</param>
        public void AddBannedIP(string ip)
        {
            if (!string.IsNullOrEmpty(ip) && !_bannedIPs.Contains(ip))
            {
                _bannedIPs.Add(ip);
            }
        }

        /// <summary>
        /// Removes an IP address from the server's ban list, allowing connections from that IP.
        /// </summary>
        /// <param name="ip">The IP address to unban. Must match exactly with a previously banned IP.</param>
        public void RemoveBannedIP(string ip)
        {
            _bannedIPs.Remove(ip);
        }

        /// <summary>
        /// Adds a CD key to the server's ban list, preventing players with that CD key from connecting.
        /// </summary>
        /// <param name="key">The CD key to ban. Use GetPCPublicCDKey() to obtain a player's CD key.</param>
        public void AddBannedCDKey(string key)
        {
            if (!string.IsNullOrEmpty(key) && !_bannedCDKeys.Contains(key))
            {
                _bannedCDKeys.Add(key);
            }
        }

        /// <summary>
        /// Removes a CD key from the server's ban list, allowing players with that CD key to connect.
        /// </summary>
        /// <param name="key">The CD key to unban. Must match exactly with a previously banned CD key.</param>
        public void RemoveBannedCDKey(string key)
        {
            _bannedCDKeys.Remove(key);
        }

        /// <summary>
        /// Adds a player name to the server's ban list, preventing players with that name from connecting.
        /// </summary>
        /// <param name="playerName">The player name to ban. Use GetPCPlayerName() to obtain a player's name.</param>
        public void AddBannedPlayerName(string playerName)
        {
            if (!string.IsNullOrEmpty(playerName) && !_bannedPlayerNames.Contains(playerName))
            {
                _bannedPlayerNames.Add(playerName);
            }
        }

        /// <summary>
        /// Removes a player name from the server's ban list, allowing that name to be used again.
        /// </summary>
        /// <param name="playerName">The player name to unban. Must match exactly with a previously banned name.</param>
        public void RemoveBannedPlayerName(string playerName)
        {
            _bannedPlayerNames.Remove(playerName);
        }

        /// <summary>
        /// Retrieves a formatted list of all banned IP addresses, CD keys, and player names.
        /// </summary>
        /// <returns>A string containing all banned items, formatted for display or logging purposes.</returns>
        public string GetBannedList()
        {
            var bannedList = new List<string>();
            
            if (_bannedIPs.Count > 0)
            {
                bannedList.Add("Banned IPs: " + string.Join(", ", _bannedIPs));
            }
            
            if (_bannedCDKeys.Count > 0)
            {
                bannedList.Add("Banned CD Keys: " + string.Join(", ", _bannedCDKeys));
            }
            
            if (_bannedPlayerNames.Count > 0)
            {
                bannedList.Add("Banned Player Names: " + string.Join(", ", _bannedPlayerNames));
            }
            
            return bannedList.Count > 0 ? string.Join("\n", bannedList) : "No banned items.";
        }

        /// <summary>
        /// Sets the module's display name as shown in the server browser and server list.
        /// </summary>
        /// <param name="name">The new name to display for the module. Cannot be null or empty.</param>
        public void SetModuleName(string name)
        {
            _moduleName = name ?? "";
        }

        /// <summary>
        /// Sets the server's display name as shown in the server browser and server list.
        /// </summary>
        /// <param name="name">The new name to display for the server. Cannot be null or empty.</param>
        public void SetServerName(string name)
        {
            _serverName = name ?? "";
        }

        /// <summary>
        /// Retrieves the current value of a specific administration option.
        /// </summary>
        /// <param name="option">The administration option to query. See AdministrationOption enum for available options.</param>
        /// <returns>True if the option is enabled, false if disabled.</returns>
        public bool GetPlayOption(AdministrationOptionType option)
        {
            return _playOptions.TryGetValue(option, out bool value) && value;
        }

        /// <summary>
        /// Sets the value of a specific administration option.
        /// </summary>
        /// <param name="option">The administration option to modify. See AdministrationOption enum for available options.</param>
        /// <param name="value">True to enable the option, false to disable it.</param>
        public void SetPlayOption(AdministrationOptionType option, bool value)
        {
            _playOptions[option] = value;
        }

        /// <summary>
        /// Deletes the Temporary User Resource Data (TURD) for a specific player and character combination.
        /// </summary>
        /// <param name="playerName">The player's account name. Must match exactly with the server's records.</param>
        /// <param name="characterName">The character's name. Must match exactly with the server's records.</param>
        /// <returns>True if the TURD was successfully deleted, false if the TURD was not found or deletion failed.</returns>
        public bool DeleteTURD(string playerName, string characterName)
        {
            // Mock implementation - always returns true for testing
            return true;
        }

        /// <summary>
        /// Retrieves the current state of a specific administration debug option.
        /// </summary>
        /// <param name="type">The debug type to query. See AdministrationDebugType enum for available types.</param>
        /// <returns>True if the debug option is enabled, false if disabled.</returns>
        public bool GetDebugValue(AdministrationDebugType type)
        {
            return _debugValues.TryGetValue(type, out bool value) && value;
        }

        /// <summary>
        /// Sets the state of a specific administration debug option.
        /// </summary>
        /// <param name="type">The debug type to modify. See AdministrationDebugType enum for available types.</param>
        /// <param name="state">True to enable debug logging for this type, false to disable it.</param>
        public void SetDebugValue(AdministrationDebugType type, bool state)
        {
            _debugValues[type] = state;
        }

        /// <summary>
        /// Retrieves the server's current minimum character level setting.
        /// </summary>
        /// <returns>The minimum character level allowed on the server.</returns>
        public int GetMinLevel()
        {
            return _minLevel;
        }

        /// <summary>
        /// Sets the server's minimum character level requirement.
        /// </summary>
        /// <param name="nLevel">The minimum character level required to connect to the server. Must be a positive integer.</param>
        public void SetMinLevel(int nLevel)
        {
            _minLevel = Math.Max(1, nLevel);
        }

        /// <summary>
        /// Retrieves the server's current maximum character level setting.
        /// </summary>
        /// <returns>The maximum character level allowed on the server.</returns>
        public int GetMaxLevel()
        {
            return _maxLevel;
        }

        /// <summary>
        /// Sets the server's maximum character level limit.
        /// </summary>
        /// <param name="nLevel">The maximum character level allowed on the server. Must be a positive integer.</param>
        public void SetMaxLevel(int nLevel)
        {
            _maxLevel = Math.Max(1, nLevel);
        }

        /// <summary>
        /// Retrieves the current server name as displayed in the server browser.
        /// </summary>
        /// <returns>The current server name as a string.</returns>
        public string GetServerName()
        {
            return _serverName;
        }

        /// <summary>
        /// Reloads the server rules from the configuration files.
        /// </summary>
        public void ReloadRules()
        {
            // Mock implementation - in real tests, this would reload configuration
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _playerPassword = "";
            _dmPassword = "";
            _moduleName = "Test Module";
            _serverName = "Test Server";
            _minLevel = 1;
            _maxLevel = 40;
            _playOptions.Clear();
            _debugValues.Clear();
            _bannedIPs.Clear();
            _bannedCDKeys.Clear();
            _bannedPlayerNames.Clear();
        }

        /// <summary>
        /// Gets the current module name for testing verification.
        /// </summary>
        /// <returns>The current module name.</returns>
        public string GetModuleName()
        {
            return _moduleName;
        }

        /// <summary>
        /// Gets all banned IPs for testing verification.
        /// </summary>
        /// <returns>A list of banned IP addresses.</returns>
        public List<string> GetBannedIPs()
        {
            return new List<string>(_bannedIPs);
        }

        /// <summary>
        /// Gets all banned CD keys for testing verification.
        /// </summary>
        /// <returns>A list of banned CD keys.</returns>
        public List<string> GetBannedCDKeys()
        {
            return new List<string>(_bannedCDKeys);
        }

        /// <summary>
        /// Gets all banned player names for testing verification.
        /// </summary>
        /// <returns>A list of banned player names.</returns>
        public List<string> GetBannedPlayerNames()
        {
            return new List<string>(_bannedPlayerNames);
        }
    }
}
