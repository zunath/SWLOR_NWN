using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
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

        public ChatTextService(
            INWScript script,
            IColorTokenService color,
            INWNXChat nwnxChat,
            IDataContext db)
        {
            _ = script;
            _color = color;
            _nwnxChat = nwnxChat;
            _db = db;
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
