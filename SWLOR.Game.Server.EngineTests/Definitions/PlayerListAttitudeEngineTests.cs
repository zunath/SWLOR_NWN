using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly List<(int ListRefresh, uint Target, uint Source, int Like)> Updates = new();
    private static readonly Dictionary<uint, CNetLayerPlayerInfo> FixtureClients = new();
    private static int _listsSent;

    [EngineTest("Name updates preserve unnamed hostility, one-sided choices, and bulk replays", Category = "PlayerList", TimeoutSeconds = 60f)]
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
            var bystanderClient = server.GetClientObjectByObjectId(bystander.Creature);
            var observerCreature = observerClient.GetGameObject().AsNWSCreature();
            var targetCreature = targetClient.GetGameObject().AsNWSCreature();
            observerCreature.AddToPVPList(target.Creature);
            targetCreature.AddToPVPList(observer.Creature);
            ctx.Assert(!PlayerName.TryGetKnownName(observer.Creature, target.Creature, out _), "Target has no assigned name");

            using var visibleTarget = new CLastUpdatePartyObject { m_nPlayerId = target.Creature };
            using var visibleObserver = new CLastUpdatePartyObject { m_nPlayerId = observer.Creature };
            var listed = false;
            var observerListed = false;
            try
            {
                BeginCapture(observerClient.m_nPlayerID, targetClient.m_nPlayerID, bystanderClient.m_nPlayerID);
                // Rename only refreshes a target already on the player list and
                // visible to the observer. Give the disconnected fixtures that state.
                server.GetNWSMessage().SendServerToPlayerPlayerList_Add(uint.MaxValue, targetClient);
                listed = true;
                server.GetNWSMessage().SendServerToPlayerPlayerList_Add(uint.MaxValue, observerClient);
                observerListed = true;
                observerClient.m_lstActivePartyObjectsLastUpdate.Add(visibleTarget);
                targetClient.m_lstActivePartyObjectsLastUpdate.Add(visibleObserver);
                // A player may Like a character who still dislikes them. Preserve
                // each direction independently rather than forcing mutual hostility.
                foreach (var outgoing in new[] { 1, 0 })
                foreach (var incoming in new[] { 1, 0 })
                {
                    targetCreature.SetPVPPlayerLikesMe(observer.Creature, outgoing, 0);
                    observerCreature.SetPVPPlayerLikesMe(target.Creature, incoming, 0);
                    Updates.Clear();
                    _listsSent = 0;
                    PlayerName.SendChatMessageWithChatNameOverride(observer.Creature, target.Creature,
                        () =>
                        {
                            ctx.AssertEqual(1, _listsSent, "Chat name applied before sending the message");
                            AssertReplay(ctx, observerClient.m_nPlayerID, targetClient.m_nPlayerID, outgoing, incoming, 1);
                        });

                    ctx.AssertEqual(2, _listsSent, "NWNX refreshed the list for chat name application and restoration");
                    AssertReplay(ctx, observerClient.m_nPlayerID, targetClient.m_nPlayerID, outgoing, incoming, 2);
                    ctx.AssertEqual(2 * ((outgoing == 0 ? 1 : 0) + (incoming == 0 ? 1 : 0)), Updates.Count,
                        "No extra attitudes are sent before or between the two refresh replays");
                    ctx.AssertEqual(outgoing, targetCreature.GetPVPPlayerLikesMe(observer.Creature), "Outgoing server attitude is unchanged");
                    ctx.AssertEqual(incoming, observerCreature.GetPVPPlayerLikesMe(target.Creature), "Incoming server attitude is unchanged");
                }

                var clients = new[] { observerClient, targetClient, bystanderClient };
                foreach (var source in clients)
                foreach (var other in clients)
                {
                    if (source.m_nPlayerID == other.m_nPlayerID) continue;
                    var creature = other.GetGameObject().AsNWSCreature();
                    creature.AddToPVPList(source.m_oidNWSObject);
                    creature.SetPVPPlayerLikesMe(source.m_oidNWSObject, 0, 0);
                }

                Updates.Clear();
                _listsSent = 0;
                PlayerName.RefreshNameOverridesForPlayer(target.Creature);
                ctx.Assert(_listsSent > 1, "Bulk overrides exercise multiple actual full-list refreshes");
                ctx.AssertEqual(clients.Length * (clients.Length - 1), Updates.Count,
                    "Bulk refresh sends each hostile direction once");
                foreach (var source in clients)
                foreach (var other in clients)
                {
                    if (source.m_nPlayerID == other.m_nPlayerID) continue;
                    ctx.Assert(Updates.Contains((_listsSent, other.m_nPlayerID, source.m_nPlayerID, 0)),
                        "Each direction is replayed after the final list refresh");
                    ctx.AssertEqual(0, other.GetGameObject().AsNWSCreature().GetPVPPlayerLikesMe(source.m_oidNWSObject),
                        "Bulk refresh leaves server hostility unchanged");
                }

                Updates.Clear();
                _listsSent = 0;
                PlayerName.RefreshNameOverridesForObserver(target.Creature);
                ctx.Assert(_listsSent > 0, "Observer-wide overrides exercise an actual full-list refresh");
                ctx.AssertEqual(2 * (clients.Length - 1), Updates.Count,
                    "Observer-wide refresh sends each of the observer's hostile directions once");
                foreach (var other in clients)
                {
                    if (other.m_nPlayerID == targetClient.m_nPlayerID) continue;
                    ctx.Assert(Updates.Contains((_listsSent, other.m_nPlayerID, targetClient.m_nPlayerID, 0)),
                        "Observer-wide outgoing hostility follows the final list refresh");
                    ctx.Assert(Updates.Contains((_listsSent, targetClient.m_nPlayerID, other.m_nPlayerID, 0)),
                        "Observer-wide incoming hostility follows the final list refresh");
                    ctx.AssertEqual(0, other.GetGameObject().AsNWSCreature().GetPVPPlayerLikesMe(target.Creature),
                        "Observer-wide refresh leaves outgoing server hostility unchanged");
                    ctx.AssertEqual(0, targetCreature.GetPVPPlayerLikesMe(other.m_oidNWSObject),
                        "Observer-wide refresh leaves incoming server hostility unchanged");
                }
            }
            finally
            {
                try
                {
                    observerClient.m_lstActivePartyObjectsLastUpdate.Remove(visibleTarget);
                    targetClient.m_lstActivePartyObjectsLastUpdate.Remove(visibleObserver);
                    if (listed)
                    {
                        try
                        {
                            try { RenamePlugin.ClearPCNameOverride(OBJECT_INVALID, target.Creature); }
                            finally { RenamePlugin.ClearPCNameOverride(target.Creature, clearAll: true); }
                        }
                        finally { server.GetNWSMessage().SendServerToPlayerPlayerList_Delete(uint.MaxValue, targetClient); }
                    }
                }
                finally
                {
                    try
                    {
                        if (observerListed)
                            server.GetNWSMessage().SendServerToPlayerPlayerList_Delete(uint.MaxValue, observerClient);
                    }
                    finally { EndCapture(); }
                }
            }
        });
    }

    private static void AssertReplay(EngineTestContext ctx, uint observer, uint target, int outgoing, int incoming, int listRefresh)
    {
        var updates = Updates.Where(update => update.ListRefresh == listRefresh).ToList();
        ctx.AssertEqual((outgoing == 0 ? 1 : 0) + (incoming == 0 ? 1 : 0), updates.Count,
            $"Refresh {listRefresh} replays only hostile directions, with no bystander or self update");
        ctx.AssertEqual(outgoing == 0, updates.Contains((listRefresh, target, observer, 0)),
            $"Refresh {listRefresh} outgoing Dislike replay");
        ctx.AssertEqual(incoming == 0, updates.Contains((listRefresh, observer, target, 0)),
            $"Refresh {listRefresh} incoming Dislike replay");
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
            Updates.Add((_listsSent, target, source, like));
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
