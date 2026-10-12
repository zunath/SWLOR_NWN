using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PlayerListAttitudeEngineTests
{
    private static unsafe FunctionHook* _attitudeHook;
    private static unsafe FunctionHook* _playerInfoHook;
    private static unsafe FunctionHook* _sendHook;
    private static readonly List<(uint Target, uint Source, int Like)> Updates = new();
    private static readonly Dictionary<uint, CNetLayerPlayerInfo> FixtureClients = new();
    private static int _listsSent;
    private static bool _attitudeBeforeList;

    [EngineTest("Name updates preserve unnamed hostility and one-sided choices", Category = "PlayerList", TimeoutSeconds = 60f)]
    public static async Task RestoreHostility(EngineTestContext ctx)
    {
        using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
        using var target = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
        using var bystander = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
        await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var observerClient = server.GetClientObjectByObjectId(observer.Creature);
            var targetClient = server.GetClientObjectByObjectId(target.Creature);
            var observerCreature = observerClient.GetGameObject().AsNWSCreature();
            var targetCreature = targetClient.GetGameObject().AsNWSCreature();
            observerCreature.AddToPVPList(target.Creature);
            targetCreature.AddToPVPList(observer.Creature);
            ctx.Assert(!PlayerName.TryGetKnownName(observer.Creature, target.Creature, out _), "Target has no assigned name");

            using var visibleTarget = new CLastUpdatePartyObject { m_nPlayerId = target.Creature };
            var listed = false;
            try
            {
                BeginCapture(observerClient.m_nPlayerID, targetClient.m_nPlayerID,
                    server.GetClientObjectByObjectId(bystander.Creature).m_nPlayerID);
                // Rename only refreshes a target already on the player list and
                // visible to the observer. Give the disconnected fixtures that state.
                server.GetNWSMessage().SendServerToPlayerPlayerList_Add(uint.MaxValue, targetClient);
                listed = true;
                observerClient.m_lstActivePartyObjectsLastUpdate.Add(visibleTarget);
                // A player may Like a character who still dislikes them. Preserve
                // each direction independently rather than forcing mutual hostility.
                foreach (var outgoing in new[] { 1, 0 })
                foreach (var incoming in new[] { 1, 0 })
                {
                    targetCreature.SetPVPPlayerLikesMe(observer.Creature, outgoing, 0);
                    observerCreature.SetPVPPlayerLikesMe(target.Creature, incoming, 0);
                    Updates.Clear();
                    _listsSent = 0;
                    _attitudeBeforeList = false;
                    PlayerName.SendChatMessageWithChatNameOverride(observer.Creature, target.Creature,
                        () => ctx.AssertEqual(1, _listsSent, "Chat name applied before sending the message"));

                    ctx.AssertEqual(2, _listsSent, "NWNX refreshed the list for chat name application and restoration");
                    ctx.Assert(!_attitudeBeforeList, "Attitudes follow the list that would otherwise reset them");
                    ctx.AssertEqual(2 * ((outgoing == 0 ? 1 : 0) + (incoming == 0 ? 1 : 0)), Updates.Count,
                        "Replay sends only hostile directions, with no bystander or self update");
                    ctx.AssertEqual(outgoing == 0,
                        Updates.Contains((targetClient.m_nPlayerID, observerClient.m_nPlayerID, 0)), "Outgoing Dislike replay");
                    ctx.AssertEqual(incoming == 0,
                        Updates.Contains((observerClient.m_nPlayerID, targetClient.m_nPlayerID, 0)), "Incoming Dislike replay");
                    ctx.AssertEqual(outgoing, targetCreature.GetPVPPlayerLikesMe(observer.Creature), "Outgoing server attitude is unchanged");
                    ctx.AssertEqual(incoming, observerCreature.GetPVPPlayerLikesMe(target.Creature), "Incoming server attitude is unchanged");
                }
            }
            finally
            {
                observerClient.m_lstActivePartyObjectsLastUpdate.Remove(visibleTarget);
                try
                {
                    if (listed)
                    {
                        RenamePlugin.ClearPCNameOverride(target.Creature, clearAll: true);
                        server.GetNWSMessage().SendServerToPlayerPlayerList_Delete(uint.MaxValue, targetClient);
                    }
                }
                finally { EndCapture(); }
            }
        });
    }

    private static unsafe void BeginCapture(params uint[] clients)
    {
        foreach (var client in clients)
            FixtureClients.Add(client, new CNetLayerPlayerInfo());
        var program = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, uint, void*> info = &GetPlayerInfo;
        _playerInfoHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
            "_ZN9CNetLayer13GetPlayerInfoEj"), (IntPtr)info, HookOrder.Early);
        delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int> packet = &SendPacket;
        _sendHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
            "_ZN11CNWSMessage25SendServerToPlayerMessageEjhhPhj"), (IntPtr)packet, HookOrder.Early);
        delegate* unmanaged<void*, uint, uint, int, int> send = &Capture;
        _attitudeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
            "_ZN11CNWSMessage37SendServerToPlayerPVP_Attitude_ChangeEjji"), (IntPtr)send, HookOrder.Early);
    }

    [UnmanagedCallersOnly]
    private static unsafe void* GetPlayerInfo(void* layer, uint player)
    {
        if (FixtureClients.TryGetValue(player, out var info)) return (void*)info.Pointer;
        return ((delegate* unmanaged<void*, uint, void*>)_playerInfoHook->m_trampoline)(layer, player);
    }

    [UnmanagedCallersOnly]
    private static unsafe int SendPacket(void* message, uint player, byte major, byte minor, byte* data, uint size)
    {
        if (FixtureClients.ContainsKey(player))
        {
            if (major == (byte)MessageMajor.PlayerList && minor == 1) _listsSent++;
            return 1; // Only the fixtures' disconnected transport is replaced.
        }
        return ((delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int>)_sendHook->m_trampoline)(message, player, major, minor, data, size);
    }

    [UnmanagedCallersOnly]
    private static unsafe int Capture(void* message, uint target, uint source, int like)
    {
        if (FixtureClients.ContainsKey(target) && FixtureClients.ContainsKey(source))
        {
            if (_listsSent == 0) _attitudeBeforeList = true;
            Updates.Add((target, source, like));
            return 1; // Disconnected fixture clients have no network transport.
        }
        return ((delegate* unmanaged<void*, uint, uint, int, int>)_attitudeHook->m_trampoline)(message, target, source, like);
    }

    private static unsafe void EndCapture()
    {
        if (_attitudeHook != null) NWNXAPI.ReturnFunctionHook(_attitudeHook);
        if (_sendHook != null) NWNXAPI.ReturnFunctionHook(_sendHook);
        if (_playerInfoHook != null) NWNXAPI.ReturnFunctionHook(_playerInfoHook);
        _attitudeHook = null;
        _sendHook = null;
        _playerInfoHook = null;
        foreach (var info in FixtureClients.Values) info.Dispose();
        FixtureClients.Clear();
        Updates.Clear();
    }
}
