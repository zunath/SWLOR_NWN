using NWN.Core.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides administrative functions for server management, player management, and server configuration.
    /// This plugin handles password management, player banning, server settings, and debug options.
    /// </summary>
    public class AdministrationPluginService : IAdministrationPluginService
    {
        /// <inheritdoc/>
        public string GetPlayerPassword()
        {
            return AdminPlugin.GetPlayerPassword();
        }

        /// <inheritdoc/>
        public void SetPlayerPassword(string password)
        {
            AdminPlugin.SetPlayerPassword(password);
        }

        /// <inheritdoc/>
        public void ClearPlayerPassword()
        {
            AdminPlugin.ClearPlayerPassword();
        }

        /// <inheritdoc/>
        public string GetDMPassword()
        {
            return AdminPlugin.GetDMPassword();
        }

        /// <inheritdoc/>
        public void SetDMPassword(string password)
        {
            AdminPlugin.SetDMPassword(password);
        }

        /// <inheritdoc/>
        public void ShutdownServer()
        {
            AdminPlugin.ShutdownServer();
        }

        /// <inheritdoc/>
        public void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "")
        {
            AdminPlugin.DeletePlayerCharacter(pc, bPreserveBackup ? 1 : 0, kickMessage);
        }

        /// <inheritdoc/>
        public void AddBannedIP(string ip)
        {
            AdminPlugin.AddBannedIP(ip);
        }

        /// <inheritdoc/>
        public void RemoveBannedIP(string ip)
        {
            AdminPlugin.RemoveBannedIP(ip);
        }

        /// <inheritdoc/>
        public void AddBannedCDKey(string key)
        {
            AdminPlugin.AddBannedCDKey(key);
        }

        /// <inheritdoc/>
        public void RemoveBannedCDKey(string key)
        {
            AdminPlugin.RemoveBannedCDKey(key);
        }

        /// <inheritdoc/>
        public void AddBannedPlayerName(string playerName)
        {
            AdminPlugin.AddBannedPlayerName(playerName);
        }

        /// <inheritdoc/>
        public void RemoveBannedPlayerName(string playerName)
        {
            AdminPlugin.RemoveBannedPlayerName(playerName);
        }

        /// <inheritdoc/>
        public string GetBannedList()
        {
            return AdminPlugin.GetBannedList();
        }

        /// <inheritdoc/>
        public void SetModuleName(string name)
        {
            AdminPlugin.SetModuleName(name);
        }

        /// <inheritdoc/>
        public void SetServerName(string name)
        {
            AdminPlugin.SetServerName(name);
        }

        /// <inheritdoc/>
        public bool GetPlayOption(AdministrationOptionType option)
        {
            int result = AdminPlugin.GetPlayOption((int)option);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetPlayOption(AdministrationOptionType option, bool value)
        {
            AdminPlugin.SetPlayOption((int)option, value ? 1 : 0);
        }

        /// <inheritdoc/>
        public bool DeleteTURD(string playerName, string characterName)
        {
            int result = AdminPlugin.DeleteTURD(playerName, characterName);
            return result != 0;
        }

        /// <inheritdoc/>
        public bool GetDebugValue(AdministrationDebugType type)
        {
            int result = AdminPlugin.GetDebugValue((int)type);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetDebugValue(AdministrationDebugType type, bool state)
        {
            AdminPlugin.SetDebugValue((int)type, state ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetMinLevel()
        {
            return AdminPlugin.GetMinLevel();
        }

        /// <inheritdoc/>
        public void SetMinLevel(int nLevel)
        {
            AdminPlugin.SetMinLevel(nLevel);
        }

        /// <inheritdoc/>
        public int GetMaxLevel()
        {
            return AdminPlugin.GetMaxLevel();
        }

        /// <inheritdoc/>
        public void SetMaxLevel(int nLevel)
        {
            AdminPlugin.SetMaxLevel(nLevel);
        }

        /// <inheritdoc/>
        public string GetServerName()
        {
            return AdminPlugin.GetServerName();
        }

        /// <inheritdoc/>
        public void ReloadRules()
        {
            AdminPlugin.ReloadRules();
        }
    }
}