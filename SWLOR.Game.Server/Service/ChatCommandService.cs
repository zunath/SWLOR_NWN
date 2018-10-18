using NWN;
using SWLOR.Game.Server.ChatCommand;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;
using System.Reflection;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class ChatCommandService : IChatCommandService
    {
        private readonly INWScript _;
        private readonly INWNXChat _nwnxChat;
        private readonly IColorTokenService _color;
        private readonly IAuthorizationService _auth;
        private readonly INWNXEvents _nwnxEvents;
        private readonly INWNXCreature _nwnxCreature;

        public ChatCommandService(
            INWNXChat nwnxChat,
            IColorTokenService color,
            IAuthorizationService auth,
            INWNXEvents nwnxEvents,
            INWNXCreature nwnxCreature,
            INWScript script)
        {
            _nwnxChat = nwnxChat;
            _color = color;
            _auth = auth;
            _nwnxEvents = nwnxEvents;
            _nwnxCreature = nwnxCreature;
            _ = script;
        }

        public void OnModuleNWNXChat(NWPlayer sender)
        {
            if (!sender.IsPlayer && !sender.IsDM) return;

            string originalMessage = _nwnxChat.GetMessage().Trim();

            if (originalMessage.Length <= 1)
                return;

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
            string command = split[0].Substring(1, split[0].Length - 1);
            split.RemoveAt(0);

            _nwnxChat.SkipMessage();

            if (!App.IsKeyRegistered<IChatCommand>("ChatCommand." + command))
            {
                sender.SendMessage(_color.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                return;
            }

            App.ResolveByInterface<IChatCommand>("ChatCommand." + command, chatCommand =>
            {
                string args = string.Join(" ", split);

                if (!chatCommand.RequiresTarget)
                {
                    ProcessChatCommand(chatCommand, sender, null, null, args);
                }
                else
                {
                    sender.SetLocalString("CHAT_COMMAND", command);
                    sender.SetLocalString("CHAT_COMMAND_ARGS", args);
                    sender.SendMessage("Please use your 'Chat Command Targeter' feat to select the target of this chat command.");

                    if (_.GetHasFeat((int) CustomFeatType.ChatCommandTargeter, sender) == FALSE)
                    {
                        _nwnxCreature.AddFeatByLevel(sender, (int)CustomFeatType.ChatCommandTargeter, 1);
                    }
                }
            });

        }

        public void OnModuleUseFeat()
        {
            NWPlayer pc = Object.OBJECT_SELF;
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();

            if (featID != (int)CustomFeatType.ChatCommandTargeter) return;

            var target = _nwnxEvents.OnFeatUsed_GetTarget();
            var targetLocation = _nwnxEvents.OnFeatUsed_GetTargetLocation();
            string command = pc.GetLocalString("CHAT_COMMAND");
            string args = pc.GetLocalString("CHAT_COMMAND_ARGS");

            if (string.IsNullOrWhiteSpace(command))
            {
                pc.SendMessage("Please enter a chat command and then use this feat. Type /help to learn more about the available chat commands.");
                return;
            }
            
            App.ResolveByInterface<IChatCommand>("ChatCommand." + command, chatCommand =>
            {
                ProcessChatCommand(chatCommand, pc, target, targetLocation, args);
            });

            pc.DeleteLocalString("CHAT_COMMAND");
            pc.DeleteLocalString("CHAT_COMMAND_ARGS");
        }


        private void ProcessChatCommand(IChatCommand command, NWPlayer sender, NWObject target, NWLocation targetLocation, string args)
        {
            if (target == null)
            {
                target = new Object();
            }

            if (targetLocation == null)
            {
                targetLocation = new Location();
            }

            CommandDetailsAttribute attribute = command.GetType().GetCustomAttribute<CommandDetailsAttribute>();
            bool isDM = sender.IsDM || _auth.IsPCRegisteredAsDM(sender);

            if (attribute != null &&
                (attribute.Permissions.HasFlag(CommandPermissionType.Player) && sender.IsPlayer ||
                 attribute.Permissions.HasFlag(CommandPermissionType.DM) && isDM))
            {
                command.DoAction(sender, target, targetLocation, args.Split(' ').ToArray());
            }
            else
            {
                sender.SendMessage(_color.Red("Invalid chat command. Use '/help' to get a list of available commands."));
            }
        }
    }

}
