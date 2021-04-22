using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Player = SWLOR.Game.Server.Core.NWNX.Player;

namespace SWLOR.Game.Server.Service
{
    public static class ChatCommand
    {
        private static readonly Dictionary<string, ChatCommandDetail> _chatCommands = new Dictionary<string, ChatCommandDetail>();
        public static string HelpTextPlayer { get; private set; }
        public static string HelpTextDM { get; private set; }
        public static string HelpTextAdmin { get; private set; }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            LoadChatCommands();
            BuildHelpText();
        }

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        [NWNEventHandler("on_nwnx_chat")]
        public static void HandleChatMessage()
        {
            var sender = OBJECT_SELF;
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

            if (!_chatCommands.ContainsKey(command))
            {
                SendMessageToPC(sender, ColorToken.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                return;
            }

            var chatCommand = _chatCommands[command];

            var args = string.Join(" ", split);

            if (!chatCommand.RequiresTarget)
            {
                ProcessChatCommand(command, sender, OBJECT_INVALID, null, args);
            }
            else
            {
                var error = chatCommand.ValidateArguments?.Invoke(sender, split.ToArray());
                if (!string.IsNullOrWhiteSpace(error))
                {
                    SendMessageToPC(sender, error);
                    return;
                }
                
                SetLocalString(sender, "CHAT_COMMAND", command);
                SetLocalString(sender, "CHAT_COMMAND_ARGS", args);
                SendMessageToPC(sender, "Please use your 'Chat Command Targeter' feat to select the target of this chat command.");

                if (!GetHasFeat(FeatType.ChatCommandTargeter, sender) || GetIsDM(sender) || GetIsDMPossessed(sender))
                {
                    Creature.AddFeatByLevel(sender, FeatType.ChatCommandTargeter, 1);

                    if (GetIsDM(sender) || GetIsDMPossessed(sender))
                    {
                        var qbs = Player.GetQuickBarSlot(sender, 11);
                        if (qbs.ObjectType == QuickBarSlotType.Empty)
                        {
                            Player.SetQuickBarSlot(sender, 11, PlayerQuickBarSlot.UseFeat(FeatType.ChatCommandTargeter));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Parse the message and ensure it starts with a slash.
        /// Sender must be a player or DM.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message sent.</param>
        /// <returns>true if this is a chat command, false otherwise</returns>
        private static bool CanHandleChat(uint sender, string message)
        {
            var validTarget = GetIsPC(sender) || GetIsDM(sender) || GetIsDMPossessed(sender);
            var validMessage = message.Length >= 2 && message[0] == '/' && message[1] != '/';
            return validTarget && validMessage;
        }

        /// <summary>
        /// Processes and runs the specific chat command entered by the user.
        /// </summary>
        /// <param name="commandName">Name of the command</param>
        /// <param name="sender">The sender object</param>
        /// <param name="target">The target of the command. OBJECT_INVALID if no target is necessary.</param>
        /// <param name="targetLocation">The target location of the command. null if no target is necessary.</param>
        /// <param name="args">User-entered arguments</param>
        private static void ProcessChatCommand(string commandName, uint sender, uint target, Location targetLocation, string args)
        {
            var command = _chatCommands[commandName];
            if (targetLocation == null)
            {
                targetLocation = new Location(IntPtr.Zero);
            }

            var authorization = Authorization.GetAuthorizationLevel(sender);

            if (command.Authorization.HasFlag(authorization))
            {
                var argsArr = string.IsNullOrWhiteSpace(args) ? new string[0] : args.Split(' ').ToArray();
                var error = command.ValidateArguments?.Invoke(sender, argsArr);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    SendMessageToPC(sender, error);
                }
                else
                {
                    command.DoAction?.Invoke(sender, target, targetLocation, argsArr);
                }
            }
            else
            {
                SendMessageToPC(sender, ColorToken.Red("Invalid chat command. Use '/help' to get a list of available commands."));
            }
        }

        /// <summary>
        /// Builds all chat commands and puts them into cache.
        /// </summary>
        private static void LoadChatCommands()
        {
            Console.WriteLine("Loading chat commands");
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IChatCommandListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IChatCommandListDefinition) Activator.CreateInstance(type);
                var commands = instance.BuildChatCommands();

                foreach (var (key, value) in commands)
                {
                    _chatCommands[key] = value;
                }
            }

            Console.WriteLine($"{_chatCommands.Count} chat commands loaded successfully.");
        }

        /// <summary>
        /// Builds text used by the /help command for each authorization level.
        /// This must be called after LoadChatCommands or there will be nothing to process.
        /// </summary>
        private static void BuildHelpText()
        {
            var orderedCommands = _chatCommands.OrderBy(o => o.Key);

            foreach (var command in orderedCommands)
            {
                var text = command.Key;
                var definition = command.Value;

                if (definition.Authorization.HasFlag(AuthorizationLevel.Player))
                {
                    HelpTextPlayer += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }

                if (definition.Authorization.HasFlag(AuthorizationLevel.DM))
                {
                    HelpTextDM += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }

                if (definition.Authorization.HasFlag(AuthorizationLevel.Admin))
                {
                    HelpTextAdmin += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }
            }
        }


        /// <summary>
        /// When a player uses the "Open Rest Menu" feat, open the rest menu dialog conversation.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void UseOpenRestMenuFeat()
        {
            var player = OBJECT_SELF;
            var feat = (FeatType)Convert.ToInt32(Events.GetEventData("FEAT_ID"));
            if (feat != FeatType.ChatCommandTargeter) return;

            var target = StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));
            var area = StringToObject(Events.GetEventData("AREA_OBJECT_ID"));
            var targetX = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_X"));
            var targetY = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Y"));
            var targetZ = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Z"));

            var targetLocation = Location(area, new Vector3(targetX, targetY, targetZ), 0.0f);
            var command = GetLocalString(player, "CHAT_COMMAND");
            var args = GetLocalString(player, "CHAT_COMMAND_ARGS");

            if (string.IsNullOrWhiteSpace(command))
            {
                SendMessageToPC(player, "Please enter a chat command and then use this feat. Type /help to learn more about the available chat commands.");
                return;
            }

            ProcessChatCommand(command, player, target, targetLocation, args);

            DeleteLocalString(player, "CHAT_COMMAND");
            DeleteLocalString(player, "CHAT_COMMAND_ARGS");
        }
    }
}
