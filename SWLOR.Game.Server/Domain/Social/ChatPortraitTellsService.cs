using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatPortraitTellService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Chat portrait tells: the logic and state that used to be static in <c>Service.ChatPortraitTells</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// See <see cref="ChatPortraitTellGuard"/> for why the client can address someone else.
    /// </summary>
    /// <remarks>
    /// The two engine lookups of connected clients are <c>protected virtual</c> so a unit test can answer them without
    /// a running server; the implementations here are the original lookups.
    /// </remarks>
    public class ChatPortraitTellsService : IChatPortraitTellsService
    {
        // A player can take a while to write the tell after clicking.
        private const int RefusalLifetimeMinutes = 10;

        private readonly ChatPortraitTellGuard _guard = new(TimeSpan.FromMinutes(RefusalLifetimeMinutes));
        // Character IDs by login object. A character that logs back in, even into the same slot, is the same speaker.
        private readonly Dictionary<uint, string> _characterIdByLoginObject = new();

        public void RememberEnteringPlayer()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // The login object can be one a previous client used, so its chat history is not this client's.
            _guard.ForgetObserver(player);
            _characterIdByLoginObject[player] = GetObjectUUID(player);
        }

        public void ForgetExitingPlayer()
        {
            var player = GetExitingObject();
            _guard.ForgetObserver(player);
            // Recorded lines keep the character ID, so it is not needed once the player is gone.
            _characterIdByLoginObject.Remove(player);
        }

        public void RecordChatLine(uint recipientSlot, uint speaker)
        {
            var speakerClient = FindClientByObjectId(speaker);
            if (speakerClient == null || speakerClient.Value.PlayerId == recipientSlot)
                return;

            var recipient = FindClientByPlayerId(recipientSlot);
            if (recipient == null)
                return;

            _guard.RecordLine(recipient.Value.LoginObject, speakerClient.Value.PlayerId, GetCharacterId(speakerClient.Value));
        }

        public long CountLinesReceived(uint player)
        {
            var client = FindClientByObjectId(player);
            return client == null ? 0 : _guard.CountLinesReceived(client.Value.LoginObject);
        }

        public void CheckPortraitClick()
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

        public ChatPortraitClick ClickPortrait(uint player, uint portraitOwner)
        {
            var observer = FindClientByObjectId(player);
            var holder = FindClientByObjectId(portraitOwner);
            if (observer == null || holder == null)
                return ChatPortraitClick.Unrecorded;

            return _guard.Click(observer.Value.LoginObject, holder.Value.PlayerId, GetCharacterId(holder.Value), DateTime.UtcNow);
        }

        public bool RefuseStalePortraitTell(uint sender, uint target, bool isDirectTell)
        {
            // A Player List tell does not send the chat prompt, so a refused click's prompt may still be open.
            if (isDirectTell)
                return false;

            var senderClient = FindClientByObjectId(sender);
            if (senderClient == null)
                return false;

            var targetClient = FindClientByObjectId(target);
            var refused = targetClient == null
                ? _guard.ConsumeRefusedTell(senderClient.Value.LoginObject, uint.MaxValue, string.Empty, DateTime.UtcNow)
                : _guard.ConsumeRefusedTell(senderClient.Value.LoginObject, targetClient.Value.PlayerId, GetCharacterId(targetClient.Value), DateTime.UtcNow);
            if (!refused)
                return false;

            SendMessageToPC(sender, ColorToken.Red("Tell not sent: the chat portrait you clicked may belong to a player who has logged out. " +
                "Use Send Tell in the Player List instead."));
            return true;
        }

        /// <summary>
        /// The connected client controlling the object, or null when no client does. Reads the engine's client list.
        /// </summary>
        protected virtual ChatPortraitTellsClient? FindClientByObjectId(uint objectId)
        {
            var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(objectId);
            return client == null ? null : new ChatPortraitTellsClient(client.m_nPlayerID, client.m_oidPCObject);
        }

        /// <summary>
        /// The connected client in the player slot, or null when the slot is empty. Reads the engine's client list.
        /// </summary>
        protected virtual ChatPortraitTellsClient? FindClientByPlayerId(uint playerId)
        {
            var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByPlayerId(playerId);
            return client == null ? null : new ChatPortraitTellsClient(client.m_nPlayerID, client.m_oidPCObject);
        }

        private string GetCharacterId(ChatPortraitTellsClient client)
        {
            return _characterIdByLoginObject.TryGetValue(client.LoginObject, out var characterId)
                ? characterId
                : $"object:{client.LoginObject:X}";
        }
    }
}
