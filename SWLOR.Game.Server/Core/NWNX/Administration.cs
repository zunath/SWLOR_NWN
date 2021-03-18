using System;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Administration
    {
        private const string PLUGIN_NAME = "NWNX_Administration";

        // Gets the current player password.
        public static string GetPlayerPassword()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlayerPassword");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Sets the current player password.
        public static void SetPlayerPassword(string password)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlayerPassword");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Removes the current player password.
        public static void ClearPlayerPassword()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearPlayerPassword");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the current DM password.
        public static string GetDMPassword()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDMPassword");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Sets the current DM password. May be set to an empty string.
        public static void SetDMPassword(string password)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDMPassword");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// Signals the server to immediately shut down.
        public static void ShutdownServer()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ShutdownServer");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Deletes the player character from the servervault
        // The PC will be immediately booted from the game with a "Delete Character" message
        public static void DeletePlayerCharacter(uint pc, bool bPreserveBackup)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeletePlayerCharacter");
            Internal.NativeFunctions.StackPushInteger(bPreserveBackup ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(pc);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// Ban a given IP - get via GetPCIPAddress()
        public static void AddBannedIP(string ip)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddBannedIP");
            Internal.NativeFunctions.StackPushString(ip);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Removes a banned IP address.
        public static void RemoveBannedIP(string ip)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveBannedIP");
            Internal.NativeFunctions.StackPushString(ip);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Adds a banned CD key. Get via GetPCPublicCDKey
        public static void AddBannedCDKey(string key)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddBannedCDKey");
            Internal.NativeFunctions.StackPushString(key);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// Removes a banned CD key.
        public static void RemoveBannedCDKey(string key)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveBannedCDKey");
            Internal.NativeFunctions.StackPushString(key);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Adds a banned player name - get via GetPCPlayerName.
        public static void AddBannedPlayerName(string playerName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddBannedPlayerName");
            Internal.NativeFunctions.StackPushString(playerName);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// Removes a banned player name.
        public static void RemoveBannedPlayerName(string playerName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveBannedPlayerName");
            Internal.NativeFunctions.StackPushString(playerName);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets a list of all banned IPs, CD Keys, and player names as a string.
        public static string GetBannedList()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBannedList");
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Sets the module's name as shown in the server list.
        public static void SetModuleName(string name)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetModuleName");
            Internal.NativeFunctions.StackPushString(name);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the server's name as shown in the server list.
        public static void SetServerName(string name)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetServerName");
            Internal.NativeFunctions.StackPushString(name);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get an AdministrationOption value
        public static int GetPlayOption(AdministrationOption option)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlayOption");
            Internal.NativeFunctions.StackPushInteger((int)option);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set an AdministrationOption value
        public static void SetPlayOption(AdministrationOption option, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlayOption");
            Internal.NativeFunctions.StackPushInteger((int)option);
            Internal.NativeFunctions.StackPushInteger(value);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Delete the temporary user resource data (TURD) of a playerName + characterName
        public static bool DeleteTURD(string playerName, string characterName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteTURD");
            Internal.NativeFunctions.StackPushString(characterName);
            Internal.NativeFunctions.StackPushString(playerName);
            Internal.NativeFunctions.nwnxCallFunction();
            return Convert.ToBoolean(Internal.NativeFunctions.nwnxPopInt());
        }

        // Get an admin_debug "Administration Debug Type" value.
        // An "Administration Debug Type"
        // The current value for the supplied debug type from admin_debug "Administration Debug Types".
        public static bool GetDebugValue(AdministrationDebugType type)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDebugValue");
            Internal.NativeFunctions.StackPushInteger((int)type);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }

        // Set an "Administration Debug Type" to a value.
        // The debug type to adjust from "Administration Debug Types".
        // The new state for the debug type, true or false
        public static void SetDebugValue(AdministrationDebugType type, bool state)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDebugValue");
            Internal.NativeFunctions.StackPushInteger(state ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)type);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the servers minimum level.
        /// @return The minimum level for the server.
        public static int GetMinLevel()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinLevel");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set the servers minimum level.
        /// @param nLevel The minimum level for the server.
        public static void SetMinLevel(int nLevel)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMinLevel");
            Internal.NativeFunctions.StackPushInteger(nLevel);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the servers maximum level.
        /// @return The maximum level for the server.
        public static int GetMaxLevel()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxLevel");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set the servers maximum level.
        /// @note Attention when using this and the MaxLevel plugin. They both change the same value.
        /// @param nLevel The maximum level for the server.
        public static void SetMaxLevel(int nLevel)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxLevel");
            Internal.NativeFunctions.StackPushInteger(nLevel);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}