using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sends a message to Discord.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Discord : IChatCommand
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly INWNXChat _nwnxChat;
        private readonly IColorTokenService _color;

        public Discord(
            INWScript script,
            IDataContext db,
            INWNXChat nwnxChat,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _nwnxChat = nwnxChat;
            _color = color;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            
            string message = string.Empty;

            foreach (var arg in args)
            {
                message += " " + arg;
            }
            
            DiscordChatQueue chatRecord = new DiscordChatQueue
            {
                DateSent = DateTime.UtcNow,
                Message = message,
                SenderName = user.Name,
                SenderAccountName = _.GetPCPlayerName(user),
                SenderCDKey = _.GetPCPublicCDKey(user)
            };

            _db.DiscordChatQueues.Add(chatRecord);
            _db.SaveChanges();

            _.DelayCommand(0.1f, () =>
            {
                NWObject chatSender = _.GetObjectByTag("Holonet");
                if (!chatSender.IsValid) return;

                foreach (var player in NWModule.Get().Players)
                {
                    if (player.GetLocalInt("DISPLAY_DISCORD") == FALSE) continue;

                    message = "[Discord] " + user.Name + ": " + message;
                    message = _color.Custom(message, 255, 218, 185);
                    _nwnxChat.SendMessage((int)ChatChannelType.PlayerTell, message, chatSender, user);
                }
            });
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            bool isEnabled = user.GetLocalInt("DISPLAY_DISCORD") == TRUE;

            if (!isEnabled)
            {
                return "You have disabled the Discord chat channel. You can't send messages to it while disabled.";
            }

            if (args.Length <= 0 || args[0].Length <= 0)
            {
                return "Please enter in a message to send to Discord.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
