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

namespace SWLOR.Game.Server.Service
{
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

        public void OnModuleChat()
        {
            int mode = _.GetPCChatVolume();

            if (mode != TALKVOLUME_SHOUT)
            {
                HandleChat();
            }
        }

        public void OnNWNXChat()
        {
            if (_nwnxChat.GetChannel() != (int)ChatChannelType.PlayerShout) return;
            NWPlayer sender = _nwnxChat.GetSender().Object;
            bool displayHolonet = sender.GetLocalInt("DISPLAY_HOLONET") == TRUE;
            string message = _nwnxChat.GetMessage();

            // Ignore chat command messages, but include OOC speech.
            if (message.Substring(0, 2) != "//" && message.Substring(0, 1) == "/")
            {
                return;
            }
            
            message = _color.Custom("[Holonet] " + sender.Name + ": " + message, 0, 180, 255);
            _nwnxChat.SkipMessage();

            if (!displayHolonet)
            {
                sender.SendMessage("You have disabled the holonet and cannot send this message.");
                return;
            }
            
            NWCreature holonetCreature = _.GetObjectByTag("Holonet");
            if (!holonetCreature.IsValid) return;
            
            _.SetPortraitId(holonetCreature, _.GetPortraitId(sender));
            
            _.DelayCommand(0.1f, () =>
            {
                foreach (var player in NWModule.Get().Players)
                {
                    displayHolonet = player.GetLocalInt("DISPLAY_HOLONET") == TRUE;

                    if (displayHolonet)
                    {
                        _nwnxChat.SendMessage((int)ChatChannelType.PlayerTell, message, holonetCreature, player);
                    }
                }
            });
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

        private void HandleChat()
        {
            string message = _.GetPCChatMessage();

            bool foundEmote = false;
            string finalText = string.Empty;
            string coloringText = string.Empty;
            foreach (var @char in message)
            {
                if (foundEmote)
                {
                    coloringText += @char;

                    if (@char == '*')
                    {
                        finalText += _color.Custom(coloringText, 200, 172, 150);
                        coloringText = string.Empty;
                        foundEmote = false;
                    }
                }
                else
                {
                    if (@char == '*')
                    {
                        coloringText += @char;
                        foundEmote = true;
                    }
                    else
                    {
                        finalText += @char;
                    }
                }
            }

            if (coloringText.Length > 0)
            {
                finalText += coloringText;
            }

            _.SetPCChatMessage(finalText);
        }
    }
}
