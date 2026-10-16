using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerListNamePrivacyEngineTests
    {
        private const uint AllGameMasters = 0xFFFFFFF6;
        private const uint AllPlayers = 0xFFFFFFF7;
        private const uint AllClients = 0xFFFFFFFF;
        private const string FirstLabel = "Private label from first observer";
        private const string SecondLabel = "Private label from second observer";
        private const string Descriptor = "A quiet traveler in a blue coat";
        private static readonly List<(uint Recipient, string Payload)> Packets = new();
        private static unsafe FunctionHook* _packetHook;

        [EngineTest("Player-list packets keep each observer's private names separate", Category = "PlayerListNamePrivacy", TimeoutSeconds = 60f)]
        public static async Task BroadcastNamesRemainPrivate(EngineTestContext ctx)
        {
            using var target = await PlayerAbilityFixture.CreateAsync(ctx);
            using var first = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var second = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var unknown = await PlayerAbilityFixture.CreateAsync(ctx, -2f);
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var dmClient = server.GetClientObjectByObjectId(dm.Creature);
            var dmCreature = server.GetCreatureByGameObjectID(dm.Creature);
            var dmPlayerInfo = server.GetNetLayer().GetPlayerInfo(dmClient.m_nPlayerID);
            var characterType = dmClient.m_nCharacterType;
            var dmCharacterFile = dmCreature.m_pStats.m_bIsDMCharacterFile;
            var dmPrivileges = dmPlayerInfo.m_bGameMasterPrivileges;
            var playerDmLogin = dmPlayerInfo.m_bGameMasterIsPlayerLogin;
            try
            {
                dmClient.m_nCharacterType = (byte)CharacterType.DM;
                dmCreature.m_pStats.m_bIsDMCharacterFile = 1;
                dmPlayerInfo.m_bGameMasterPrivileges = 1;
                dmPlayerInfo.m_bGameMasterIsPlayerLogin = 0;
                ctx.Assert(GetIsDM(dm.Creature), "staff observer uses the native DM branch");
                foreach (var observer in new[] { first, second })
                    observer.Update(player => player.Settings.ShowDescriptorsForNamedPlayers = false);
                PlayerDescriptor.SetUnknownDisplayName(target.Creature, Descriptor);
                PlayerName.SetKnownName(first.Creature, target.Creature, FirstLabel);
                PlayerName.SetKnownName(second.Creature, target.Creature, SecondLabel);
                PlayerName.RefreshNameOverridesForPlayer(target.Creature);
                var canonicalName = GetName(target.Creature);
                var expected = new Dictionary<uint, string>
                {
                    [ClientId(first)] = FirstLabel,
                    [ClientId(second)] = SecondLabel,
                    [ClientId(unknown)] = Descriptor,
                    [ClientId(target)] = canonicalName,
                    [ClientId(dm)] = canonicalName
                };

                StartCapture();
                AssertPackets(AllPlayers, expected.Keys.Where(id => id != ClientId(dm)).ToArray());
                AssertPackets(AllGameMasters, new[] { ClientId(dm) });
                AssertPackets(AllClients, expected.Keys.ToArray());
                AssertPackets(ClientId(first), new[] { ClientId(first) });

                PlayerName.ForgetKnownName(first.Creature, target.Creature);
                expected[ClientId(first)] = Descriptor;
                AssertPackets(AllPlayers, expected.Keys.Where(id => id != ClientId(dm)).ToArray());
                ctx.AssertEqual(canonicalName, GetName(target.Creature), "native Rename restores canonical identity after every recipient");
                ctx.SetResultDetail("Captured actual native player-list add packets after NWNX Rename for two private labels, an unknown observer, self and a DM. Verified player/DM/all-client broadcasts, a targeted update and forgetting a label.");

                void AssertPackets(uint recipient, uint[] expectedRecipients)
                {
                    Packets.Clear();
                    var nativeTarget = server.GetClientObjectByObjectId(target.Creature);
                    ctx.AssertEqual(1, server.GetNWSMessage().SendServerToPlayerPlayerList_Add(recipient, nativeTarget), "native add result");
                    ctx.AssertEqual(expectedRecipients.Length, Packets.Count, "one separately serialized packet per recipient");
                    ctx.Assert(Packets.Select(packet => packet.Recipient).OrderBy(id => id)
                        .SequenceEqual(expectedRecipients.OrderBy(id => id)), "native recipient group is preserved");
                    foreach (var packet in Packets)
                    {
                        ctx.Assert(packet.Payload.Contains(expected[packet.Recipient], StringComparison.Ordinal),
                            $"recipient {packet.Recipient} receives their own resolved identity");
                        if (packet.Recipient != ClientId(first) || expected[packet.Recipient] != FirstLabel)
                            ctx.Assert(!packet.Payload.Contains(FirstLabel, StringComparison.Ordinal), "first private label never reaches another observer");
                        if (packet.Recipient != ClientId(second))
                            ctx.Assert(!packet.Payload.Contains(SecondLabel, StringComparison.Ordinal), "second private label never reaches another observer");
                    }
                }

                uint ClientId(PlayerAbilityFixture player) => server.GetClientObjectByObjectId(player.Creature).m_nPlayerID;
            }
            finally
            {
                StopCapture();
                dmClient.m_nCharacterType = characterType;
                dmCreature.m_pStats.m_bIsDMCharacterFile = dmCharacterFile;
                dmPlayerInfo.m_bGameMasterPrivileges = dmPrivileges;
                dmPlayerInfo.m_bGameMasterIsPlayerLogin = playerDmLogin;
                foreach (var observer in new[] { first, second })
                    foreach (var names in DB.Search(new DBQuery<PlayerKnownName>()
                                 .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false)
                                 .AddPaging(50, 0)).ToList())
                        DB.Delete<PlayerKnownName>(names.Id);
            }
        }

        private static unsafe void StartCapture()
        {
            delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int> send = &CapturePacket;
            _packetHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage25SendServerToPlayerMessageEjhhPhj"), (IntPtr)send, HookOrder.Late);
        }

        private static unsafe void StopCapture()
        {
            if (_packetHook != null) NWNXAPI.ReturnFunctionHook(_packetHook);
            _packetHook = null;
            Packets.Clear();
        }

        [UnmanagedCallersOnly]
        private static unsafe int CapturePacket(void* message, uint recipient, byte major, byte minor, byte* data, uint size)
        {
            // Capture the actual serialized bytes without needing connected network clients.
            if (major == 10 && minor == 2)
            {
                Packets.Add((recipient, Encoding.UTF8.GetString(new ReadOnlySpan<byte>(data, checked((int)size)))));
                return 1;
            }
            var original = (delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int>)_packetHook->m_trampoline;
            return original(message, recipient, major, minor, data, size);
        }
    }
}
