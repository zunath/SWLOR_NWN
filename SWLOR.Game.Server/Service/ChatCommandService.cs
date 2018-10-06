using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.ChatCommand;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ChatCommandService: IChatCommandService
    {
        private readonly INWNXChat _nwnxChat;
        private readonly IColorTokenService _color;
        private readonly IAuthorizationService _auth;

        public ChatCommandService(
            INWNXChat nwnxChat, 
            IColorTokenService color,
            IAuthorizationService auth)
        {
            _nwnxChat = nwnxChat;
            _color = color;
            _auth = auth;
        }

        public void OnModuleNWNXChat(NWPlayer sender)
        {
            if (!sender.IsPlayer || sender.IsDM) return;

            string originalMessage = _nwnxChat.GetMessage().Trim();
            

            // If it is double slash (//) treat it as a normal message (this is used by role-players to denote OOC speech)
            if (originalMessage.Substring(0, 2) == "//") return;

            if (originalMessage.Substring(0, 1) != "/")
            {
                return;
            }

            var split = originalMessage.Split(' ').ToList();

            // Commands with no arguments won't be split, so if we didn't split anything then add the command to the split list manually.
            if (split.Count <= 0)
                split.Add(originalMessage);

            split[0] = split[0].ToLower();
            string command = split[0].Substring(1, split[0].Length-1);
            _nwnxChat.SkipMessage();

            if (!App.IsKeyRegistered<IChatCommand>("ChatCommand." + command))
            {
                sender.SendMessage(_color.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                return;
            }

            App.ResolveByInterface<IChatCommand>("ChatCommand." + command, chatCommand =>
            {
                CommandDetailsAttribute attribute = chatCommand.GetType().GetCustomAttribute<CommandDetailsAttribute>();
                bool isDM = _auth.IsPCRegisteredAsDM(sender);

                if (attribute != null &&
                    (attribute.Permissions.HasFlag(CommandPermissionType.Player) && sender.IsPlayer ||
                     attribute.Permissions.HasFlag(CommandPermissionType.DM) && isDM))
                {
                    split.RemoveAt(0);
                    chatCommand.DoAction(sender, split.ToArray());
                }
                else
                {
                    sender.SendMessage(_color.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                }
            });
            
        }
    }
}
