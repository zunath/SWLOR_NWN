using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class AdministrationPlugin
    {
        private static IAdministrationPluginService _service = new AdministrationPluginService();

        internal static void SetService(IAdministrationPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IAdministrationPluginService.GetPlayerPassword"/>
        public static string GetPlayerPassword() => _service.GetPlayerPassword();

        /// <inheritdoc cref="IAdministrationPluginService.SetPlayerPassword"/>
        public static void SetPlayerPassword(string password) => _service.SetPlayerPassword(password);

        /// <inheritdoc cref="IAdministrationPluginService.ClearPlayerPassword"/>
        public static void ClearPlayerPassword() => _service.ClearPlayerPassword();

        /// <inheritdoc cref="IAdministrationPluginService.GetDMPassword"/>
        public static string GetDMPassword() => _service.GetDMPassword();

        /// <inheritdoc cref="IAdministrationPluginService.SetDMPassword"/>
        public static void SetDMPassword(string password) => _service.SetDMPassword(password);

        /// <inheritdoc cref="IAdministrationPluginService.ShutdownServer"/>
        public static void ShutdownServer() => _service.ShutdownServer();

        /// <inheritdoc cref="IAdministrationPluginService.DeletePlayerCharacter"/>
        public static void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "") => _service.DeletePlayerCharacter(pc, bPreserveBackup, kickMessage);

        /// <inheritdoc cref="IAdministrationPluginService.AddBannedIP"/>
        public static void AddBannedIP(string ip) => _service.AddBannedIP(ip);

        /// <inheritdoc cref="IAdministrationPluginService.RemoveBannedIP"/>
        public static void RemoveBannedIP(string ip) => _service.RemoveBannedIP(ip);

        /// <inheritdoc cref="IAdministrationPluginService.AddBannedCDKey"/>
        public static void AddBannedCDKey(string key) => _service.AddBannedCDKey(key);

        /// <inheritdoc cref="IAdministrationPluginService.RemoveBannedCDKey"/>
        public static void RemoveBannedCDKey(string key) => _service.RemoveBannedCDKey(key);

        /// <inheritdoc cref="IAdministrationPluginService.AddBannedPlayerName"/>
        public static void AddBannedPlayerName(string playerName) => _service.AddBannedPlayerName(playerName);

        /// <inheritdoc cref="IAdministrationPluginService.RemoveBannedPlayerName"/>
        public static void RemoveBannedPlayerName(string playerName) => _service.RemoveBannedPlayerName(playerName);

        /// <inheritdoc cref="IAdministrationPluginService.GetBannedList"/>
        public static string GetBannedList() => _service.GetBannedList();

        /// <inheritdoc cref="IAdministrationPluginService.SetModuleName"/>
        public static void SetModuleName(string name) => _service.SetModuleName(name);

        /// <inheritdoc cref="IAdministrationPluginService.SetServerName"/>
        public static void SetServerName(string name) => _service.SetServerName(name);

        /// <inheritdoc cref="IAdministrationPluginService.GetPlayOption"/>
        public static bool GetPlayOption(AdministrationOptionType option) => _service.GetPlayOption(option);

        /// <inheritdoc cref="IAdministrationPluginService.SetPlayOption"/>
        public static void SetPlayOption(AdministrationOptionType option, bool value) => _service.SetPlayOption(option, value);

        /// <inheritdoc cref="IAdministrationPluginService.DeleteTURD"/>
        public static bool DeleteTURD(string playerName, string characterName) => _service.DeleteTURD(playerName, characterName);

        /// <inheritdoc cref="IAdministrationPluginService.GetDebugValue"/>
        public static bool GetDebugValue(AdministrationDebugType type) => _service.GetDebugValue(type);

        /// <inheritdoc cref="IAdministrationPluginService.SetDebugValue"/>
        public static void SetDebugValue(AdministrationDebugType type, bool state) => _service.SetDebugValue(type, state);

        /// <inheritdoc cref="IAdministrationPluginService.GetMinLevel"/>
        public static int GetMinLevel() => _service.GetMinLevel();

        /// <inheritdoc cref="IAdministrationPluginService.SetMinLevel"/>
        public static void SetMinLevel(int nLevel) => _service.SetMinLevel(nLevel);

        /// <inheritdoc cref="IAdministrationPluginService.GetMaxLevel"/>
        public static int GetMaxLevel() => _service.GetMaxLevel();

        /// <inheritdoc cref="IAdministrationPluginService.SetMaxLevel"/>
        public static void SetMaxLevel(int nLevel) => _service.SetMaxLevel(nLevel);

        /// <inheritdoc cref="IAdministrationPluginService.GetServerName"/>
        public static string GetServerName() => _service.GetServerName();

        /// <inheritdoc cref="IAdministrationPluginService.ReloadRules"/>
        public static void ReloadRules() => _service.ReloadRules();
    }
}
