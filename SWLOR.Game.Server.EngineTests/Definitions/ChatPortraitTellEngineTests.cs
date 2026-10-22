using System.Diagnostics;
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
                // The Player List tell left the stale click's prompt unsent, so that prompt is still refused.
                ctx.Assert(ChatPortraitTells.RefuseStalePortraitTell(observer.Creature, second.Creature, false),
                    "the stale prompt sent after a player list tell");
            }
            finally
            {
                first.Dispose();
                second?.Dispose();
            }
        }

        [EngineTest("Chat portrait history records every hooked engine chat send and every player chat path", Category = "PlayerName", TimeoutSeconds = 90f)]
        public static async Task EveryChatPathRecordsThePortraitSpeaker(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            // Inside the 4-unit whisper range, so every nearby channel reaches the observer.
            using var speaker = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var observerSlot = GetSlot(observer.Creature);

            // Each hooked native function, called through its own export so its hook runs with the real calling convention.
            var engineSends = new (string Name, Action<CNWSMessage, CExoString> Send)[]
            {
                ("Talk", (message, text) => message.SendServerToPlayerChat_Talk(observerSlot, speaker.Creature, text)),
                ("Whisper", (message, text) => message.SendServerToPlayerChat_Whisper(observerSlot, speaker.Creature, text)),
                ("Party", (message, text) => message.SendServerToPlayerChat_Party(observerSlot, speaker.Creature, text)),
                ("Tell", (message, text) => message.SendServerToPlayerChat_Tell(observerSlot, speaker.Creature, text)),
                ("Shout", (message, text) => message.SendServerToPlayerChat_Shout(observerSlot, speaker.Creature, text)),
                ("Silent Shout", (message, text) => message.SendServerToPlayerChat_Silent_Shout(observerSlot, speaker.Creature, text)),
                ("DM Talk", (message, text) => message.SendServerToPlayerChat_DM_Talk(observerSlot, speaker.Creature, text)),
                ("DM Whisper", (message, text) => message.SendServerToPlayerChat_DM_Whisper(observerSlot, speaker.Creature, text)),
                ("DM Silent Shout", (message, text) => message.SendServerToPlayerChat_DM_Silent_Shout(observerSlot, speaker.Creature, text)),
            };
            foreach (var (name, send) in engineSends)
            {
                var before = ChatPortraitTells.CountLinesReceived(observer.Creature);
                await ctx.ExecuteInCreatureContextAsync(speaker.Creature, () =>
                {
                    using var text = new CExoString("engine " + name);
                    send(NWNXLib.g_pAppManager.m_pServerExoApp.GetNWSMessage(), text);
                });
                ctx.AssertEqual(before + 1, ChatPortraitTells.CountLinesReceived(observer.Creature), name + " line recorded");
            }

            // What players actually send: the client's chat messages through SWLOR's chat handling, and SpeakString
            // (dice rolls). The speaker's own echo is never counted for the observer.
            var playerSends = new (string Name, Func<Task> Send)[]
            {
                ("client Talk", () => SendClientChatAsync(ctx, speaker.Creature, 1, "client talk")),
                ("client Whisper", () => SendClientChatAsync(ctx, speaker.Creature, 3, "client whisper")),
                ("client Party", () => SendClientChatAsync(ctx, speaker.Creature, 6, "client comms")),
                ("client Tell", () => SendClientTellAsync(ctx, speaker.Creature, observerSlot, "client tell")),
                ("SpeakString", () => ctx.ExecuteInCreatureContextAsync(speaker.Creature, () => SpeakString("rolls a die"))),
            };
            foreach (var (name, send) in playerSends)
            {
                var before = ChatPortraitTells.CountLinesReceived(observer.Creature);
                await send();
                await ctx.WaitUntilAsync(() => ChatPortraitTells.CountLinesReceived(observer.Creature) > before, 5f, name + " line recorded");
            }

            ctx.AssertEqual(ChatPortraitClick.CurrentHolder, ChatPortraitTells.ClickPortrait(observer.Creature, speaker.Creature),
                "every line came from the speaker who holds the slot");
            ctx.AssertEqual(0L, ChatPortraitTells.CountLinesReceived(speaker.Creature), "a speaker's own lines are not recorded for them");
        }

        [EngineTest("Chat portrait tell is refused when the speaker logs out while it is being written", Category = "PlayerName", TimeoutSeconds = 60f)]
        public static async Task PortraitTellRefusedWhenSlotChangesHandsWhileWriting(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            var first = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            PlayerAbilityFixture second = null;
            try
            {
                PlayerDescriptor.SetUnknownDisplayName(first.Creature, SharedDescriptor);
                var sharedSlot = GetSlot(first.Creature);
                await SendClientTellAsync(ctx, first.Creature, GetSlot(observer.Creature), "first line");

                // The click is current: the first speaker still holds the slot.
                await SendClientPortraitClickAsync(ctx, observer.Creature, first.Creature);

                // Before the tell is sent, the first speaker logs out and another character takes the slot.
                first.Dispose();
                second = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
                PlayerDescriptor.SetUnknownDisplayName(second.Creature, SharedDescriptor);
                ctx.AssertEqual(sharedSlot, GetSlot(second.Creature), "the second player takes the freed slot");

                await SendClientTellAsync(ctx, observer.Creature, sharedSlot, "meant for the first speaker");
                ctx.AssertEqual(0L, ChatPortraitTells.CountLinesReceived(second.Creature), "the tell never reaches the new holder");

                // A tell the observer then types to the new holder is their own choice and is delivered.
                await SendClientTellAsync(ctx, observer.Creature, sharedSlot, "typed to the new holder");
                ctx.AssertEqual(1L, ChatPortraitTells.CountLinesReceived(second.Creature), "the typed tell reaches the new holder");
            }
            finally
            {
                first.Dispose();
                second?.Dispose();
            }
        }

        [EngineTest("Chat portrait hook cost per chat line", Category = "PlayerName", TimeoutSeconds = 90f)]
        public static async Task ChatPortraitHookCost(EngineTestContext ctx)
        {
            const int Lines = 5000;
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var speaker = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var observerSlot = GetSlot(observer.Creature);
            var before = ChatPortraitTells.CountLinesReceived(observer.Creature);
            double hookedMicroseconds = 0, unhookedMicroseconds = 0;

            await ctx.ExecuteInCreatureContextAsync(speaker.Creature, () =>
            {
                var message = NWNXLib.g_pAppManager.m_pServerExoApp.GetNWSMessage();
                using var text = new CExoString("measure");
                // Warm both paths so JIT and first-use costs stay out of the timing.
                for (var line = 0; line < 100; line++)
                {
                    message.SendServerToPlayerChat_Talk(observerSlot, speaker.Creature, text);
                    message.SendServerToPlayerChat_ServerTell(observerSlot, text);
                }

                var timer = Stopwatch.StartNew();
                for (var line = 0; line < Lines; line++)
                    message.SendServerToPlayerChat_Talk(observerSlot, speaker.Creature, text);
                hookedMicroseconds = timer.Elapsed.TotalMilliseconds * 1000 / Lines;

                // A similar per-recipient chat send this change does not hook, as the engine's own cost.
                timer.Restart();
                for (var line = 0; line < Lines; line++)
                    message.SendServerToPlayerChat_ServerTell(observerSlot, text);
                unhookedMicroseconds = timer.Elapsed.TotalMilliseconds * 1000 / Lines;
            });

            ctx.Log($"Hooked Talk send: {hookedMicroseconds:F2} us per line; unhooked ServerTell send: {unhookedMicroseconds:F2} us per line; " +
                    $"difference {hookedMicroseconds - unhookedMicroseconds:F2} us.");
            ctx.AssertEqual(before + Lines + 100, ChatPortraitTells.CountLinesReceived(observer.Creature), "every timed line recorded");
            // A Comms line to 100 players stays under a millisecond of hook time.
            ctx.Assert(hookedMicroseconds - unhookedMicroseconds < 10,
                $"hook adds {hookedMicroseconds - unhookedMicroseconds:F2} us per chat line");
        }

        internal static uint GetSlot(uint creature)
        {
            return NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(creature).m_nPlayerID;
        }

        /// <summary>Hands the engine a tell addressed by player ID, the way the client sends one.</summary>
        internal static Task SendClientTellAsync(EngineTestContext ctx, uint sender, uint targetSlot, string text)
        {
            return ctx.ExecuteInCreatureContextAsync(sender, () => Receive(sender, message =>
            {
                using var exoText = new CExoString(text);
                message.WriteBOOL(0);
                message.WriteDWORD(targetSlot, 32);
                message.WriteCExoString(exoText, 32);
            }, (server, client) => server.HandlePlayerToServerChatMessage(client, 4)));
        }

        /// <summary>Hands the engine a Talk, Whisper or Party line the way the client sends one (minor 1, 3 or 6).</summary>
        private static Task SendClientChatAsync(EngineTestContext ctx, uint sender, byte minor, string text)
        {
            return ctx.ExecuteInCreatureContextAsync(sender, () => Receive(sender, message =>
            {
                using var exoText = new CExoString(text);
                message.WriteCExoString(exoText, 32);
            }, (server, client) => server.HandlePlayerToServerChatMessage(client, minor)));
        }

        /// <summary>Hands the engine the GUI event the client sends when a chat-log portrait is clicked.</summary>
        internal static Task SendClientPortraitClickAsync(EngineTestContext ctx, uint player, uint portraitOwner)
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
