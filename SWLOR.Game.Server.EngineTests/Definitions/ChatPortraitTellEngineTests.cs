using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.ChatPortraitTellService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ChatPortraitTellEngineTests
    {
        private const string SharedDescriptor = "A recovery specialist";

        [EngineTest("Chat portrait tell never reaches the player who took a logged-out speaker's slot", Category = "PlayerName", TimeoutSeconds = 60f)]
        public static async Task PortraitTellFollowsTheSpeaker(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            var first = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            PlayerAbilityFixture second = null;
            try
            {
                PlayerDescriptor.SetUnknownDisplayName(first.Creature, SharedDescriptor);
                var sharedSlot = GetSlot(first.Creature);

                // The first speaker's line reaches the observer through the engine's own tell path.
                await SendClientTellAsync(ctx, first.Creature, GetSlot(observer.Creature), "first line");
                ctx.AssertEqual(ChatPortraitClick.CurrentHolder, ChatPortraitTells.ClickPortrait(observer.Creature, first.Creature),
                    "a portrait whose speaker is still online");

                // The first speaker logs out and a different character with the same descriptor takes the slot.
                first.Dispose();
                second = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
                PlayerDescriptor.SetUnknownDisplayName(second.Creature, SharedDescriptor);
                ctx.AssertEqual(sharedSlot, GetSlot(second.Creature), "the second player takes the freed slot");

                // The observer clicks the first speaker's old portrait; the client resolves it to the slot's new holder.
                await SendClientPortraitClickAsync(ctx, observer.Creature, second.Creature);
                await SendClientTellAsync(ctx, observer.Creature, sharedSlot, "meant for the first speaker");
                ctx.AssertEqual(ChatPortraitClick.Unrecorded, ChatPortraitTells.ClickPortrait(second.Creature, observer.Creature),
                    "the tell from the stale portrait never reaches the new holder");

                // The Player List names its target exactly, so it still reaches the new holder after a stale click.
                ctx.AssertEqual(ChatPortraitClick.FormerHolder, ChatPortraitTells.ClickPortrait(observer.Creature, second.Creature),
                    "the stale portrait click");
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    ctx.AssertEqual(string.Empty, PlayerList.SendTell(observer.Creature, second.Creature, "chosen from the player list"),
                        "player list tell"));
                ctx.AssertEqual(ChatPortraitClick.CurrentHolder, ChatPortraitTells.ClickPortrait(second.Creature, observer.Creature),
                    "the player list tell reaches the new holder");
            }
            finally
            {
                first.Dispose();
                second?.Dispose();
            }
        }

        private static uint GetSlot(uint creature)
        {
            return NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(creature).m_nPlayerID;
        }

        /// <summary>Hands the engine a tell addressed by player ID, the way the client sends one.</summary>
        private static Task SendClientTellAsync(EngineTestContext ctx, uint sender, uint targetSlot, string text)
        {
            return ctx.ExecuteInCreatureContextAsync(sender, () => Receive(sender, message =>
            {
                using var exoText = new CExoString(text);
                message.WriteBOOL(0);
                message.WriteDWORD(targetSlot, 32);
                message.WriteCExoString(exoText, 32);
            }, (server, client) => server.HandlePlayerToServerChatMessage(client, 4)));
        }

        /// <summary>Hands the engine the GUI event the client sends when a chat-log portrait is clicked.</summary>
        private static Task SendClientPortraitClickAsync(EngineTestContext ctx, uint player, uint portraitOwner)
        {
            return ctx.ExecuteInCreatureContextAsync(player, () => Receive(player, message =>
            {
                message.WriteWORD((ushort)GuiEventType.ChatLogPortraitClick, 16);
                message.WriteWORD(0, 16);
                // The headless client fails the build check that adds the event's vector.
                message.WriteDWORD(portraitOwner, 32);
            }, (server, client) => server.HandlePlayerToServerGuiEvent(client, 1)));
        }

        private static unsafe void Receive(uint player, Action<CNWSMessage> write, Func<CNWSMessage, CNWSPlayer, int> handle)
        {
            var app = NWNXLib.g_pAppManager.m_pServerExoApp;
            var client = app.GetClientObjectByObjectId(player);
            var server = app.GetNWSMessage();
            // The engine's message object is larger than the SWIG wrapper's allocation.
            var memory = NativeMemory.AllocZeroed(0x70);
            var construct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageC1Ev");
            var destruct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageD1Ev");
            construct(memory);
            var writer = CNWSMessage.FromPointer(memory);
            try
            {
                writer.CreateWriteMessage(64, 0xffffffff, 1);
                write(writer);
                byte* bytes = null;
                uint size = 0;
                writer.GetWriteMessage(&bytes, &size);
                // GetWriteMessage includes the three-byte transport header the engine strips from incoming messages.
                server.SetReadMessage(bytes + 3, size - 3, client.m_nPlayerID, 1);
                handle(server, client);
            }
            finally
            {
                server.SetReadMessage(null, 0, client.m_nPlayerID, 1);
                destruct(memory);
                NativeMemory.Free(memory);
                writer.Dispose();
            }
        }
    }
}
