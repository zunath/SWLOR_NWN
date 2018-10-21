using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using System.Text;

namespace SWLOR.Game.Server.Service
{
    public struct ChatComponent
    {
        public string m_Text;
        public bool m_Translatable;
        public bool m_CustomColour;
        public byte m_ColourRed;
        public byte m_ColourGreen;
        public byte m_ColourBlue;
    };

    public enum EmoteMode
    {
        Regular,
        Novel
    };

    public class ChatTextService : IChatTextService
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly INWNXChat _nwnxChat;
        private readonly IDataContext _db;
        private readonly HttpClient _httpClient;

        public ChatTextService(
            INWScript script,
            IColorTokenService color,
            INWNXChat nwnxChat,
            IDataContext db,
            HttpClient httpClient)
        {
            _ = script;
            _color = color;
            _nwnxChat = nwnxChat;
            _db = db;
            _httpClient = httpClient;
        }

        public void OnNWNXChat()
        {
            ChatChannelType channel = (ChatChannelType)_nwnxChat.GetChannel();

            // So we're going to play with a couple of channels here.

            // - PlayerTalk, PlayerWhisper, PlayerParty, and PlayerShout are all IC channels. These channels
            //   are subject to emote colouring and language translation. (see below for more info).
            // - PlayerParty is an IC channel with special behaviour. Those outside of the party but within
            //   range may listen in to the party chat. (see below for more information).
            // - PlayerShout sends a holocom message server-wide through the DMTell channel.
            // - PlayerDM echoes back the message received to the sender.

            bool inCharacterChat =
                channel == ChatChannelType.PlayerTalk ||
                channel == ChatChannelType.PlayerWhisper ||
                channel == ChatChannelType.PlayerParty ||
                channel == ChatChannelType.PlayerShout;

            bool messageToDm = channel == ChatChannelType.PlayerDM;

            if (!inCharacterChat && !messageToDm)
            {
                // We don't much care about traffic on the other channels.
                return;
            }

            NWObject sender = _nwnxChat.GetSender();
            string message = _nwnxChat.GetMessage();

            if (ChatCommandService.CanHandleChat(sender, message) ||
                BaseService.CanHandleChat(sender, message) ||
                CraftService.CanHandleChat(sender, message))
            {
                // This will be handled by other services, so just bail.
                return;
            }

            if (channel == ChatChannelType.PlayerDM)
            {
                // Simply echo the message back to the player.
                _nwnxChat.SendMessage((int)ChatChannelType.ServerMessage, "(Sent to DM) " + message, sender, sender);
                return;
            }

            // At this point, every channel left is one we want to manually handle.
            _nwnxChat.SkipMessage();

            // If this is a party message, and the holonet is disabled, we disallow it.
            if (channel == ChatChannelType.PlayerParty && sender.IsPlayer && sender.GetLocalInt("DISPLAY_HOLONET") == FALSE)
            {
                NWPlayer player = sender.Object;
                player.SendMessage("You have disabled the holonet and cannot send this message.");
                return;
            }

            // TODO - Assume emote mode is regular.
            List<ChatComponent> chatComponents;
            EmoteMode emoteMode = EmoteMode.Regular;

            // Quick early out - if we start with "//" or "((", this is an OOC message.
            if (message.Substring(0, 2) == "//" || message.Substring(0, 2) == "((")
            {
                ChatComponent component = new ChatComponent
                {
                    m_Text = message,
                    m_CustomColour = true,
                    m_ColourRed = 64,
                    m_ColourGreen = 64,
                    m_ColourBlue = 64,
                    m_Translatable = false
                };

                chatComponents = new List<ChatComponent>();
                chatComponents.Add(component);
            }
            else
            {
                if (emoteMode == EmoteMode.Regular)
                {
                    chatComponents = SplitMessageIntoComponents_Regular(message);
                }
                else
                {
                    chatComponents = SplitMessageIntoComponents_Novel(message);
                }
            }

            // Now, depending on the chat channel, we need to build a list of recipients.
            bool needsAreaCheck = false;
            float distanceCheck = 0.0f;

            List<NWObject> recipients = new List<NWObject>();

            // The sender always wants to see their own message.
            recipients.Add(sender);

            // This is a server-wide holonet message (that receivers can toggle on or off).
            if (channel == ChatChannelType.PlayerShout)
            {
                recipients.AddRange(NWModule.Get().Players.Where(player => player.GetLocalInt("DISPLAY_HOLONET") == TRUE));
            }
            // This is the normal party chat, plus everyone within 20 units of the sender.
            else if (channel == ChatChannelType.PlayerParty)
            {
                // Can an NPC use the playerparty channel? I feel this is safe ...
                NWPlayer player = sender.Object;
                recipients.AddRange(player.PartyMembers.Where(pm => !pm.Equals(sender)).Cast<NWObject>());
                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Normal talk - 20 units.
            else if (channel == ChatChannelType.PlayerTalk)
            {
                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Whisper - 4 units.
            else if (channel == ChatChannelType.PlayerWhisper)
            {
                needsAreaCheck = true;
                distanceCheck = 4.0f;
            }

            if (needsAreaCheck)
            {
                recipients.AddRange(sender.Area.Objects.Where(obj => !obj.Equals(sender) && obj.IsPlayer && _.GetDistanceBetween(sender, obj) <= distanceCheck));
                // TODO: Need to add DMs too, Area.Objects doesn't return them. We need to cache them on entry.
            }

            // Now we have a list of who is going to actually receive a message, we need to modify
            // the message for each recipient then dispatch them.

            foreach (NWObject obj in recipients)
            {
                // Generate the final message as perceived by obj.

                StringBuilder finalMessage = new StringBuilder();

                if (channel == ChatChannelType.PlayerShout)
                {
                    finalMessage.Append("[Holonet] ");
                }
                else if (channel == ChatChannelType.PlayerParty)
                {
                    finalMessage.Append("[Comms] ");
                }

                // TODO - append language name if not basic.

                foreach (ChatComponent component in chatComponents)
                {
                    string text = component.m_Text;

                    if (component.m_Translatable)
                    {
                        // TODO - Translate from translation service.
                    }

                    if (component.m_CustomColour)
                    {
                        text = _color.Custom(text, component.m_ColourRed, component.m_ColourGreen, component.m_ColourBlue);
                    }

                    finalMessage.Append(text);
                }

                // Dispatch the final message - method depends on the original chat channel.
                // - Shout is sent as talk.
                // - Party is sent as talk.
                // - Talk is sent as talk.
                // - Whisper is sent as whisper.

                ChatChannelType finalChannel = channel;

                if (channel == ChatChannelType.PlayerShout || channel == ChatChannelType.PlayerParty)
                {
                    finalChannel = ChatChannelType.PlayerTalk;
                }

                // There are a couple of colour overrides we want to use here.
                // - One for holonet (shout).
                // - One for comms (party chat).

                string finalMessageColoured = finalMessage.ToString();

                if (channel == ChatChannelType.PlayerShout)
                {
                    finalMessageColoured = _color.Custom(finalMessageColoured, 0, 180, 255);
                }
                else if (channel == ChatChannelType.PlayerParty)
                {
                    finalMessageColoured = _color.Yellow(finalMessageColoured);
                }
            
                _nwnxChat.SendMessage((int)finalChannel, finalMessageColoured, sender, obj);
            }
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            player.SetLocalInt("DISPLAY_HOLONET", dbPlayer.DisplayHolonet ? TRUE : FALSE);
            player.SetLocalInt("DISPLAY_DISCORD", dbPlayer.DisplayDiscord ? TRUE : FALSE);
        }

        public void OnModuleHeartbeat()
        {
            List<DiscordChatQueue> discordQueue = _db
                .DiscordChatQueues
                .Where(x => x.DatePosted == null || x.DatePosted == null && x.DateForRetry != null && DateTime.UtcNow > x.DateForRetry && x.RetryAttempts < 10)
                .OrderBy(o => o.DateForRetry)
                .ThenBy(o => o.DateSent)
                .ToList();

            foreach (var queue in discordQueue)
            {
                PostAsync(queue.DiscordChatQueueID, queue.SenderName, queue.Message, queue.SenderAccountName);
                queue.DatePosted = DateTime.UtcNow;
            }

            _db.SaveChanges();
        }

        private List<ChatComponent> SplitMessageIntoComponents_Regular(string message)
        {
            // TODO

            List<ChatComponent> components = new List<ChatComponent>();

            ChatComponent component = new ChatComponent
            {
                m_Text = message,
                m_CustomColour = false,
                m_Translatable = false
            };

            components.Add(component);

            return components;
        }

        private List<ChatComponent> SplitMessageIntoComponents_Novel(string message)
        {
            // TODO
        
            List<ChatComponent> components = new List<ChatComponent>();

            ChatComponent component = new ChatComponent
            {
                m_Text = message,
                m_CustomColour = false,
                m_Translatable = false
            };

            components.Add(component);

            return components;
        }

        private async void PostAsync(int queueID, string characterName, string message, string accountName)
        {
            string url = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
            if (string.IsNullOrWhiteSpace(url)) return;

            dynamic data = new ExpandoObject();
            data.content = message;
            data.username = characterName + " (" + accountName + ")";
            data.file = message;

            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.NoContent:
                    return;
            }
            
            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (responseContent.Length <= 0) return;

                dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

                Console.WriteLine(responseContent);

                App.Resolve<IDataContext>(db =>
                {
                    var record = db.DiscordChatQueues.Single(x => x.DiscordChatQueueID == queueID);
                    record.DatePosted = null;

                    if (responseContent.Length > 0)
                    {
                        record.ResponseContent = responseContent;
                    }

                    record.RetryAttempts++;

                    int retryMS = 60000; // 1 minute
                    if (responseData != null && responseData.retry_after != null)
                    {
                        retryMS = responseData.retry_after;
                    }

                    record.DateForRetry = DateTime.UtcNow.AddMilliseconds(retryMS);
                    db.SaveChanges();
                });
            }
        }
    }
}
