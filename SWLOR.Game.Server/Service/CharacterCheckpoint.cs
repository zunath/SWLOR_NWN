using SWLOR.Game.Server.Service.LogService;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Saves a character file now. The ordinary export command queues a later save (<see cref="Migration.SavePlayerFileCheckpoint"/>), so
    /// anything that must reach the file before a database write that depends on it saves through this instead.
    /// </summary>
    public static class CharacterCheckpoint
    {
        /// <summary>Saves the player's character file. False when it could not be saved.</summary>
        public static bool Save(uint player)
        {
            try
            {
                var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(player);
                return client != null && client.SaveServerCharacter() != 0;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Character checkpoint failed for player [{GetObjectUUID(player)}]: {ex.Message}");
                return false;
            }
        }
    }
}
