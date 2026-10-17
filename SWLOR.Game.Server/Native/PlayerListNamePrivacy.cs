using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Serializes player-list additions separately for each observer's private names.</summary>
    public static unsafe class PlayerListNamePrivacy
    {
        private const uint AllGameMasters = 0xFFFFFFF6;
        private const uint AllPlayers = 0xFFFFFFF7;
        private const uint AllClients = 0xFFFFFFFF;
        private static FunctionHook* _addHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_addHook != null) return;
            delegate* unmanaged<void*, uint, void*, int> add = &SendPlayerListAdd;
            // Run outside NWNX Rename's Early hook. Its broadcast path applies every
            // observer's name to one creature, then serializes only the last name once.
            _addHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage32SendServerToPlayerPlayerList_AddEjP10CNWSPlayer"),
                (IntPtr)add, HookOrder.Earliest);
        }

        [UnmanagedCallersOnly]
        private static int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer)
        {
            try
            {
                var original = (delegate* unmanaged<void*, uint, void*, int>)_addHook->m_trampoline;
                var players = NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList()
                    .Select(player => (player.m_nPlayerID, player.m_nCharacterType == (byte)CharacterType.DM));
                return Dispatch(recipientId, players, observerId => original(message, observerId, newPlayer));
            }
            catch (Exception exception)
            {
                // Never fall back to a broadcast containing another observer's private name.
                Log.WriteError(exception, "Could not send private player-list additions");
                return 0;
            }
        }

        internal static int Dispatch(uint recipientId, IEnumerable<(uint PlayerId, bool IsDM)> players,
            Func<uint, int> send)
        {
            if (recipientId != AllPlayers && recipientId != AllGameMasters && recipientId != AllClients)
                return send(recipientId);

            // Snapshot before sending: native hooks may inspect or update the player list.
            var recipients = players
                .Where(player => recipientId == AllClients || player.IsDM == (recipientId == AllGameMasters))
                .Select(player => player.PlayerId)
                .ToArray();
            var result = 1;
            foreach (var observerId in recipients)
                result &= send(observerId);
            return result;
        }
    }
}
