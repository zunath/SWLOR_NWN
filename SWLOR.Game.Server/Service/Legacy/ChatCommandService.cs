using System;
using System.Collections.Generic;
using SWLOR.Game.Server.ChatCommand;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;


using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class ChatCommandService
    {
        private static readonly Dictionary<string, IChatCommand> _chatCommands;

        static ChatCommandService()
        {
            _chatCommands = new Dictionary<string, IChatCommand>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        public static bool CanHandleChat(NWObject sender, string message)
        {
            var validTarget = sender.IsPlayer || sender.IsDM;
            var validMessage = message.Length >= 2 && message[0] == '/' && message[1] != '/';
            return validTarget && validMessage;
        }

        private static void OnModuleLoad()
        {
            RegisterChatCommandHandlers();
        }


        private static void RegisterChatCommandHandlers()
        {
            // Use reflection to get all of IChatCommand handler implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IChatCommand).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();

            foreach (var type in classes)
            {
                var instance = Activator.CreateInstance(type) as IChatCommand;
                
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                // We use the lower-case class name as the key because later on we do a lookup based on text entered by the player.
                var key = type.Name.ToLower();
                _chatCommands.Add(key, instance);
            }
        }

        private static bool IsChatCommandRegistered(string commandName)
        {
            commandName = commandName.ToLower();
            return _chatCommands.ContainsKey(commandName);
        }

        private static IChatCommand GetChatCommandHandler(string commandName)
        {
            commandName = commandName.ToLower();
            if (!_chatCommands.ContainsKey(commandName))
            {
                throw new ArgumentException("Chat command handler '" + commandName + "' is not registered.");
            }

            return _chatCommands[commandName];
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer sender = NWScript.OBJECT_SELF;
            var originalMessage = Chat.GetMessage().Trim();

            if (!CanHandleChat(sender, originalMessage))
            {
                return;
            }

            var split = originalMessage.Split(' ').ToList();

            // Commands with no arguments won't be split, so if we didn't split anything then add the command to the split list manually.
            if (split.Count <= 0)
                split.Add(originalMessage);

            split[0] = split[0].ToLower();
            var command = split[0].Substring(1, split[0].Length - 1);
            split.RemoveAt(0);

            Chat.SkipMessage();

            if (!IsChatCommandRegistered(command))
            {
                sender.SendMessage(ColorTokenService.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                return;
            }

            var chatCommand = GetChatCommandHandler(command);
            var args = string.Join(" ", split);

            if (!chatCommand.RequiresTarget)
            {
                ProcessChatCommand(chatCommand, sender, null, null, args);
            }
            else
            {
                var error = chatCommand.ValidateArguments(sender, split.ToArray());
                if (!string.IsNullOrWhiteSpace(error))
                {
                    sender.SendMessage(error);
                    return;
                }

                sender.SetLocalString("CHAT_COMMAND", command);
                sender.SetLocalString("CHAT_COMMAND_ARGS", args);
                sender.SendMessage("Please use your 'Chat Command Targeter' feat to select the target of this chat command.");

                if (NWScript.GetHasFeat(Feat.ChatCommandTargeter, sender) || sender.IsDM)
                {
                    Creature.AddFeatByLevel(sender, Feat.ChatCommandTargeter, 1);

                    if(sender.IsDM)
                    {
                        var qbs = Player.GetQuickBarSlot(sender, 11);
                        if (qbs.ObjectType == QuickBarSlotType.Empty)
                        {
                            Player.SetQuickBarSlot(sender, 11, PlayerQuickBarSlot.UseFeat(Feat.ChatCommandTargeter));
                        }
                    }
                }
            }
        

        }

        private static void OnModuleUseFeat()
        {
            NWPlayer pc = NWScript.OBJECT_SELF;
            var featID = Convert.ToInt32(Events.GetEventData("FEAT_ID")); 

            if (featID != (int)Feat.ChatCommandTargeter) return;

            var target = StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));
            var targetPositionX = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Z"));
            var targetPosition = Vector3(targetPositionX, targetPositionY, targetPositionZ);
            var targetArea = StringToObject( Events.GetEventData("AREA_OBJECT_ID"));

            var targetLocation = Location(targetArea, targetPosition, 0.0f);
            var command = pc.GetLocalString("CHAT_COMMAND");
            var args = pc.GetLocalString("CHAT_COMMAND_ARGS");

            if (string.IsNullOrWhiteSpace(command))
            {
                pc.SendMessage("Please enter a chat command and then use this feat. Type /help to learn more about the available chat commands.");
                return;
            }

            var chatCommand = GetChatCommandHandler(command);
            ProcessChatCommand(chatCommand, pc, target, targetLocation, args);
            
            pc.DeleteLocalString("CHAT_COMMAND");
            pc.DeleteLocalString("CHAT_COMMAND_ARGS");
        }


        private static void ProcessChatCommand(IChatCommand command, NWPlayer sender, NWObject target, NWLocation targetLocation, string args)
        {
            if (target == null)
            {
                target = NWScript.OBJECT_INVALID;
            }

            if (targetLocation == null)
            {
                targetLocation = sender.Location;
            }

            var attribute = command.GetType().GetCustomAttribute<CommandDetailsAttribute>();
            var authorization = AuthorizationService.GetDMAuthorizationType(sender);

            if (attribute != null &&
                (attribute.Permissions.HasFlag(CommandPermissionType.Player) && authorization == DMAuthorizationType.None ||
                 attribute.Permissions.HasFlag(CommandPermissionType.DM) && authorization == DMAuthorizationType.DM ||
                 attribute.Permissions.HasFlag(CommandPermissionType.Admin) && authorization == DMAuthorizationType.Admin))
            {
                var argsArr = string.IsNullOrWhiteSpace(args) ? new string[0] : args.Split(' ').ToArray();
                var error = command.ValidateArguments(sender, argsArr);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    sender.SendMessage(error);
                }
                else
                {
                    command.DoAction(sender, target, targetLocation, argsArr);
                }
            }
            else
            {
                sender.SendMessage(ColorTokenService.Red("Invalid chat command. Use '/help' to get a list of available commands."));
            }
        }
    }

}
