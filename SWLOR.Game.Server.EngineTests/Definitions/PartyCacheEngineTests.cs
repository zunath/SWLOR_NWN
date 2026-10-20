using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Drives the engine's own party message handler, as a client's party controls do, and checks that
    /// the Party service's caches follow the engine's party membership.
    /// </summary>
    public static class PartyCacheEngineTests
    {
        private const byte PartyMajor = 0x0E;
        private const byte KickMinor = 0x07;
        private const byte InviteMinor = 0x09;
        private const byte AcceptInvitationMinor = 0x0B;
        private const uint ListTypeMask = 0x80000000;
        private static readonly List<(uint Recipient, byte[] Payload)> PartyPackets = new();
        private static unsafe FunctionHook* _packetHook;

        [EngineTest("An invitation acceptance the engine refuses leaves the party caches unchanged", Category = "PartyCache", TimeoutSeconds = 60f)]
        public static async Task RefusedAcceptanceDoesNotJoinCachedParty(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            using var invited = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var uninvited = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            try
            {
                await JoinPartyAsync(ctx, leader.Creature, invited.Creature);

                // No pending invitation, as with one that expired before the player clicked Accept.
                await SendPartyMessageAsync(ctx, uninvited.Creature, AcceptInvitationMinor, leader.Creature);
                ctx.Assert(!GetFactionEqual(uninvited.Creature, leader.Creature), "the engine refuses an acceptance without an invitation");
                ctx.Assert(!Party.IsInParty(leader.Creature, uninvited.Creature), "the refused player is not cached in the leader's party");
                ctx.AssertEqual(1, Party.GetAllPartyMembers(uninvited.Creature).Count, "the refused player has no cached party");
                ctx.AssertEqual(2, Party.GetAllPartyMembers(leader.Creature).Count, "the leader's cached party keeps its two members");
                ctx.SetResultDetail("Native invite and accept joined the engine party and the cache; an accept with no invitation was refused by the engine and left the cache unchanged.");
            }
            finally
            {
                await LeavePartiesAsync(ctx, leader.Creature, invited.Creature, uninvited.Creature);
            }
        }

        [EngineTest("A kicked player is removed from the party caches", Category = "PartyCache", TimeoutSeconds = 60f)]
        public static async Task KickRemovesPlayerFromCachedParty(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            using var stays = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var kicked = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            try
            {
                await JoinPartyAsync(ctx, leader.Creature, stays.Creature);
                await JoinPartyAsync(ctx, leader.Creature, kicked.Creature);

                // Only the leader may kick, so the engine refuses this one.
                await SendPartyMessageAsync(ctx, stays.Creature, KickMinor, kicked.Creature);
                ctx.Assert(GetFactionEqual(leader.Creature, kicked.Creature), "the engine refuses a kick from a member who is not the leader");
                ctx.Assert(Party.IsInParty(leader.Creature, kicked.Creature), "a refused kick keeps the player in the cached party");

                await SendPartyMessageAsync(ctx, leader.Creature, KickMinor, kicked.Creature);
                ctx.Assert(!GetFactionEqual(leader.Creature, kicked.Creature), "the engine removes the kicked player from the party");
                ctx.Assert(!Party.IsInParty(leader.Creature, kicked.Creature), "the kicked player is removed from the cached party");
                ctx.AssertEqual(1, Party.GetAllPartyMembers(kicked.Creature).Count, "the kicked player has no cached party");
                ctx.Assert(Party.GetAllPartyMembers(leader.Creature).OrderBy(id => id)
                        .SequenceEqual(new[] { leader.Creature, stays.Creature }.OrderBy(id => id)),
                    "the remaining members stay in the cached party");
                ctx.SetResultDetail("A native kick from a non-leader was refused and kept the cache; the leader's native kick removed the player from the engine party and the cache.");
            }
            finally
            {
                await LeavePartiesAsync(ctx, leader.Creature, stays.Creature, kicked.Creature);
            }
        }

        [EngineTest("Making a player leader from the Player List sends every member the new party order", Category = "PartyCache", TimeoutSeconds = 60f)]
        public static async Task MakeLeaderUpdatesMembersPartyLists(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            using var newLeader = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var member = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var members = new[] { leader.Creature, newLeader.Creature, member.Creature };
            try
            {
                await JoinPartyAsync(ctx, leader.Creature, newLeader.Creature);
                await JoinPartyAsync(ctx, leader.Creature, member.Creature);
                ctx.AssertEqual(leader.Creature, GetFactionLeader(member.Creature), "the inviter starts as the party leader");

                StartCapture();
                var transferred = false;
                await ctx.ExecuteInCreatureContextAsync(leader.Creature,
                    () => transferred = PlayerList.MakeLeader(leader.Creature, newLeader.Creature));
                ctx.Assert(transferred, "the engine accepts the leadership transfer");
                ctx.AssertEqual(newLeader.Creature, GetFactionLeader(member.Creature), "the engine names the new leader");

                var server = NWNXLib.g_pAppManager.m_pServerExoApp;
                foreach (var recipient in members)
                {
                    var clientId = server.GetClientObjectByObjectId(recipient).m_nPlayerID;
                    var lists = PartyPackets.Where(packet => packet.Recipient == clientId).ToList();
                    ctx.Assert(lists.Count > 0, $"{GetName(recipient)} receives a party list update");
                    var payload = lists.Last().Payload;
                    var newLeaderAt = IndexOfObject(payload, newLeader.Creature);
                    var oldLeaderAt = IndexOfObject(payload, leader.Creature);
                    // Each client's list leaves out its own creature; the leader comes first among the rest.
                    if (recipient != newLeader.Creature)
                        ctx.Assert(newLeaderAt >= 0 && (oldLeaderAt < 0 || newLeaderAt < oldLeaderAt),
                            "the new leader leads the party list a member receives");
                }

                ctx.SetResultDetail("PlayerList.MakeLeader transferred native leadership and every member's client was sent a party list led by the new leader.");
            }
            finally
            {
                StopCapture();
                await LeavePartiesAsync(ctx, members);
            }
        }

        [EngineTest("A party member who is not the leader can invite from the Player List", Category = "PartyCache", TimeoutSeconds = 60f)]
        public static async Task MemberInviteFromPlayerListJoinsTheParty(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            using var member = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            using var outsider = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            try
            {
                await JoinPartyAsync(ctx, leader.Creature, member.Creature);
                ctx.AssertEqual(leader.Creature, GetFactionLeader(member.Creature), "the member does not lead the party");

                var relation = PlayerList.GetRelation(member.Creature, outsider.Creature);
                ctx.Assert(relation.ViewerIsInParty && !relation.ViewerIsPartyLeader, "the viewer is a member who is not the leader");
                ctx.Assert(PlayerListRules.Evaluate(relation).Invite.IsEnabled, "the Player List lets the member invite");

                await ctx.ExecuteInCreatureContextAsync(member.Creature, () => PlayerList.Invite(member.Creature, outsider.Creature));
                await SendPartyMessageAsync(ctx, outsider.Creature, AcceptInvitationMinor, member.Creature);

                ctx.Assert(GetFactionEqual(outsider.Creature, leader.Creature), "the engine puts the invited player in the member's party");
                ctx.AssertEqual(leader.Creature, GetFactionLeader(outsider.Creature), "the original leader still leads");
                ctx.Assert(Party.IsInParty(leader.Creature, outsider.Creature), "the invited player is cached in the same party");
                ctx.AssertEqual(3, Party.GetAllPartyMembers(leader.Creature).Count, "the cached party has three members");
                ctx.SetResultDetail("A non-leader's Player List invite reached the engine, and accepting it joined the existing party under the same leader.");
            }
            finally
            {
                await LeavePartiesAsync(ctx, leader.Creature, member.Creature, outsider.Creature);
            }
        }

        private static async Task JoinPartyAsync(EngineTestContext ctx, uint inviter, uint invited)
        {
            await SendPartyMessageAsync(ctx, inviter, InviteMinor, invited);
            await SendPartyMessageAsync(ctx, invited, AcceptInvitationMinor, inviter);
            ctx.Assert(GetFactionEqual(inviter, invited), "the engine accepts the invitation");
            ctx.Assert(Party.IsInParty(inviter, invited), "the accepted player is cached in the inviter's party");
        }

        /// <summary>
        /// Hands the engine a party message as if the player's client sent it, so the NWNX party events fire.
        /// </summary>
        private static Task SendPartyMessageAsync(EngineTestContext ctx, uint sender, byte minor, uint target)
        {
            return ctx.ExecuteInCreatureContextAsync(sender, () => SendPartyMessage(sender, minor, target));
        }

        private static unsafe void SendPartyMessage(uint sender, byte minor, uint target)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var player = server.GetClientObjectByObjectId(sender);
            var message = server.GetNWSMessage();
            // The read buffer opens with its byte-section length plus 3, then the object ID, then one bit-fragment byte.
            var buffer = (byte*)NativeMemory.AllocZeroed(9);
            try
            {
                *(uint*)buffer = 8 + 3;
                *(uint*)(buffer + 4) = target;
                message.SetReadMessage(buffer, 9, player.m_nPlayerID, 1);
                message.HandlePlayerToServerParty(player, minor);
            }
            finally
            {
                message.SetReadMessage(null, 0, player.m_nPlayerID, 1);
                NativeMemory.Free(buffer);
            }
        }

        private static async Task LeavePartiesAsync(EngineTestContext ctx, params uint[] players)
        {
            foreach (var player in players)
            {
                if (GetIsObjectValid(player))
                    await ctx.ExecuteInCreatureContextAsync(player, () => Party.RemoveFromCurrentParty(player));
            }
        }

        private static int IndexOfObject(byte[] payload, uint objectId)
        {
            var bytes = System.BitConverter.GetBytes(objectId | ListTypeMask);
            for (var index = 0; index + bytes.Length <= payload.Length; index++)
            {
                if (payload.AsSpan(index, bytes.Length).SequenceEqual(bytes))
                    return index;
            }

            return -1;
        }

        private static unsafe void StartCapture()
        {
            PartyPackets.Clear();
            delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int> send = &CapturePacket;
            _packetHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage25SendServerToPlayerMessageEjhhPhj"), (System.IntPtr)send, HookOrder.Late);
        }

        private static unsafe void StopCapture()
        {
            if (_packetHook != null) NWNXAPI.ReturnFunctionHook(_packetHook);
            _packetHook = null;
            PartyPackets.Clear();
        }

        [UnmanagedCallersOnly]
        private static unsafe int CapturePacket(void* message, uint recipient, byte major, byte minor, byte* data, uint size)
        {
            // Headless clients have no connection, so keep the serialized party packets instead of sending them.
            if (major == PartyMajor)
            {
                PartyPackets.Add((recipient, new System.ReadOnlySpan<byte>(data, checked((int)size)).ToArray()));
                return 1;
            }

            var original = (delegate* unmanaged<void*, uint, byte, byte, byte*, uint, int>)_packetHook->m_trampoline;
            return original(message, recipient, major, minor, data, size);
        }
    }
}
