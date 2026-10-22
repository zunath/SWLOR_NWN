using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ChatPortraitTellService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Keeps a tell started by clicking a chat-log portrait from reaching a player other than the one whose line
    /// carried that portrait. See <see cref="ChatPortraitTellGuard"/> for why the client can address someone else.
    /// </summary>
    public static class ChatPortraitTells
    {
        // A player can take a while to write the tell after clicking.
        private static readonly TimeSpan RefusalLifetime = TimeSpan.FromMinutes(10);
        private static readonly ChatPortraitTellGuard Guard = new(RefusalLifetime);
        // Character IDs by login object. A character that logs back in, even into the same slot, is the same speaker.
        private static readonly Dictionary<uint, string> CharacterIdByLoginObject = new();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RememberEnteringPlayer()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // The login object can be one a previous client used, so its chat history is not this client's.
            Guard.ForgetObserver(player);
            CharacterIdByLoginObject[player] = GetObjectUUID(player);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetExitingPlayer()
        {
            var player = GetExitingObject();
            Guard.ForgetObserver(player);
            // Recorded lines keep the character ID, so it is not needed once the player is gone.
            CharacterIdByLoginObject.Remove(player);
        }

        /// <summary>
        /// Records a chat line the engine is sending to one client. Called from a native hook, so this only
        /// reads engine and managed state.
        /// </summary>
        /// <param name="recipientSlot">The receiving client's player ID.</param>
        /// <param name="speaker">The object that spoke the line.</param>
        internal static void RecordChatLine(uint recipientSlot, uint speaker)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var speakerClient = server.GetClientObjectByObjectId(speaker);
            if (speakerClient == null || speakerClient.m_nPlayerID == recipientSlot)
                return;

            var recipient = server.GetClientObjectByPlayerId(recipientSlot);
            if (recipient == null)
                return;

            Guard.RecordLine(recipient.m_oidPCObject, speakerClient.m_nPlayerID, GetCharacterId(speakerClient));
        }

        /// <summary>How many chat lines from other players have reached the player's client this session.</summary>
        public static long CountLinesReceived(uint player)
        {
            var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(player);
            return client == null ? 0 : Guard.CountLinesReceived(client.m_oidPCObject);
        }

        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
        public static void CheckPortraitClick()
        {
            if (GetLastGuiEventType() != GuiEventType.ChatLogPortraitClick)
                return;

            var player = GetLastGuiEventPlayer();
            var result = ClickPortrait(player, GetLastGuiEventObject());
            if (result is ChatPortraitClick.FormerHolder or ChatPortraitClick.Ambiguous)
            {
                SendMessageToPC(player, ColorToken.Red("That chat portrait may belong to a player who has logged out, so it could now point to someone else. " +
                    "A tell started from it will not be sent. Use Send Tell in the Player List instead."));
            }
        }

        /// <summary>
        /// Classifies a chat-log portrait click by the clicking player and the player the client resolved it to.
        /// </summary>
        public static ChatPortraitClick ClickPortrait(uint player, uint portraitOwner)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var observer = server.GetClientObjectByObjectId(player);
            var holder = server.GetClientObjectByObjectId(portraitOwner);
            if (observer == null || holder == null)
                return ChatPortraitClick.Unrecorded;

            return Guard.Click(observer.m_oidPCObject, holder.m_nPlayerID, GetCharacterId(holder), DateTime.UtcNow);
        }

        /// <summary>
        /// Whether to refuse a tell because a portrait click that may belong to another player started it, telling
        /// the sender why. A tell from the Player List names its target exactly and is never refused.
        /// </summary>
        public static bool RefuseStalePortraitTell(uint sender, uint target, bool isDirectTell)
        {
            // A Player List tell does not send the chat prompt, so a refused click's prompt may still be open.
            if (isDirectTell)
                return false;

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var senderClient = server.GetClientObjectByObjectId(sender);
            if (senderClient == null)
                return false;

            var targetClient = server.GetClientObjectByObjectId(target);
            var refused = targetClient == null
                ? Guard.ConsumeRefusedTell(senderClient.m_oidPCObject, uint.MaxValue, string.Empty, DateTime.UtcNow)
                : Guard.ConsumeRefusedTell(senderClient.m_oidPCObject, targetClient.m_nPlayerID, GetCharacterId(targetClient), DateTime.UtcNow);
            if (!refused)
                return false;

            SendMessageToPC(sender, ColorToken.Red("Tell not sent: the chat portrait you clicked may belong to a player who has logged out. " +
                "Use Send Tell in the Player List instead."));
            return true;
        }

        private static string GetCharacterId(CNWSPlayer client)
        {
            return CharacterIdByLoginObject.TryGetValue(client.m_oidPCObject, out var characterId)
                ? characterId
                : $"object:{client.m_oidPCObject:X}";
        }
    }
}
