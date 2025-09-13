using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.Core;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class AdministrationPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Administration";

        // Gets the current player password.
        public static string GetPlayerPassword()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlayerPassword");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Sets the current player password.
        public static void SetPlayerPassword(string password)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlayerPassword");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Removes the current player password.
        public static void ClearPlayerPassword()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearPlayerPassword");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the current DM password.
        public static string GetDMPassword()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDMPassword");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Sets the current DM password. May be set to an empty string.
        public static void SetDMPassword(string password)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDMPassword");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// Signals the server to immediately shut down.
        public static void ShutdownServer()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ShutdownServer");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Deletes the player character from the servervault
        // The PC will be immediately booted from the game with a "Delete Character" message
        public static void DeletePlayerCharacter(uint pc, bool bPreserveBackup = true, string kickMessage = "")
        {
            // NativeFunction calls don't work for this method and cause a crash. Use the core methods instead.
            NWNXCore.NWNX_PushArgumentString(kickMessage);
            NWNXCore.NWNX_PushArgumentInt(bPreserveBackup ? 1 : 0);
            NWNXCore.NWNX_PushArgumentObject(pc);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, "DeletePlayerCharacter");
        }

        /// Ban a given IP - get via GetPCIPAddress()
        public static void AddBannedIP(string ip)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddBannedIP");
            VM.StackPush(ip);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Removes a banned IP address.
        public static void RemoveBannedIP(string ip)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveBannedIP");
            VM.StackPush(ip);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Adds a banned CD key. Get via GetPCPublicCDKey
        public static void AddBannedCDKey(string key)
        {
            NWNXCore.NWNX_PushArgumentString(key);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, "AddBannedCDKey");
        }

        /// Removes a banned CD key.
        public static void RemoveBannedCDKey(string key)
        {
            NWNXCore.NWNX_PushArgumentString(key);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, "RemoveBannedCDKey");
        }

        // Adds a banned player name - get via GetPCPlayerName.
        public static void AddBannedPlayerName(string playerName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddBannedPlayerName");
            VM.StackPush(playerName);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// Removes a banned player name.
        public static void RemoveBannedPlayerName(string playerName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveBannedPlayerName");
            VM.StackPush(playerName);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets a list of all banned IPs, CD Keys, and player names as a string.
        public static string GetBannedList()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBannedList");
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Sets the module's name as shown in the server list.
        public static void SetModuleName(string name)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetModuleName");
            VM.StackPush(name);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the server's name as shown in the server list.
        public static void SetServerName(string name)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetServerName");
            VM.StackPush(name);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get an AdministrationOption value
        public static bool GetPlayOption(AdministrationOption option)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlayOption");
            VM.StackPush((int)option);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        // Set an AdministrationOption value
        public static void SetPlayOption(AdministrationOption option, bool value)
        {
            // NativeFunction calls don't work for this method and cause a crash. Use the core methods instead.
            NWNXCore.NWNX_PushArgumentInt(value ? 1 : 0);
            NWNXCore.NWNX_PushArgumentInt((int)option);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, "SetPlayOption");
        }

        // Delete the temporary user resource data (TURD) of a playerName + characterName
        public static bool DeleteTURD(string playerName, string characterName)
        {
            NWNXCore.NWNX_PushArgumentString(characterName);
            NWNXCore.NWNX_PushArgumentString(playerName);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, "DeleteTURD");
            return NWNXCore.NWNX_GetReturnValueInt() == 1;
        }

        // Get an admin_debug "Administration Debug Type" value.
        // An "Administration Debug Type"
        // The current value for the supplied debug type from admin_debug "Administration Debug Types".
        public static bool GetDebugValue(AdministrationDebugType type)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDebugValue");
            VM.StackPush((int)type);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        // Set an "Administration Debug Type" to a value.
        // The debug type to adjust from "Administration Debug Types".
        // The new state for the debug type, true or false
        public static void SetDebugValue(AdministrationDebugType type, bool state)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDebugValue");
            VM.StackPush(state ? 1 : 0);
            VM.StackPush((int)type);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the servers minimum level.
        /// @return The minimum level for the server.
        public static int GetMinLevel()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinLevel");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set the servers minimum level.
        /// @param nLevel The minimum level for the server.
        public static void SetMinLevel(int nLevel)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMinLevel");
            VM.StackPush(nLevel);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the servers maximum level.
        /// @return The maximum level for the server.
        public static int GetMaxLevel()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxLevel");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set the servers maximum level.
        /// @note Attention when using this and the MaxLevel plugin. They both change the same value.
        /// @param nLevel The maximum level for the server.
        public static void SetMaxLevel(int nLevel)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxLevel");
            VM.StackPush(nLevel);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}