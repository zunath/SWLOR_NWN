using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>Serializes player-list additions separately for each observer's private names.</summary>
public sealed unsafe class PlayerListNamePrivacySink : IPlayerListNamePrivacySink
{
    private const uint AllGameMasters = 0xFFFFFFF6;
    private const uint AllPlayers = 0xFFFFFFF7;
    private const uint AllClients = 0xFFFFFFFF;

    public int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer, nint original)
    {
        try
        {
            var function = (delegate* unmanaged<void*, uint, void*, int>)original;
            var players = NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList()
                .Select(player => (player.m_nPlayerID, player.m_nCharacterType == (byte)CharacterType.DM));
            return Dispatch(recipientId, players, observerId => function(message, observerId, newPlayer));
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
