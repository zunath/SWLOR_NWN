using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service;
using Dialog = SWLOR.Game.Server.Service.Dialog;
using Player = SWLOR.Game.Server.Core.NWNX.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class ChatCommand
    {
        /// <summary>
        /// These flags determine which types of players can use each chat command.
        /// </summary>
        [Flags]
        public enum CommandPermissionType
        {
            Player = 1,
            DM = 2,
            Admin = 4
        }

        /// <summary>
        /// This is a delegate which is executed when a chat command is run.
        /// </summary>
        /// <param name="user">The user activating the chat command</param>
        /// <param name="target">The target of the chat command.</param>
        /// <param name="targetLocation">The target location of the chat command.</param>
        /// <param name="args">Any arguments sent in by the user. Be sure to validate these in the ValidateArgumentsDelegate.</param>
        public delegate void ExecuteChatCommandDelegate(uint user, uint target, Location targetLocation, params string[] args);

        /// <summary>
        /// This is a delegate which is executed when a chat command is validated.
        /// </summary>
        /// <param name="user">The user activating the chat command.</param>
        /// <param name="args">Any arguments sent in by the user. Be sure to validate these here.</param>
        /// <returns>If successful, return a null or empty string. Otherwise, return the error message.</returns>
        public delegate string ValidateArgumentsDelegate(uint user, params string[] args);

        /// <summary>
        /// Defines a chat command.
        /// </summary>
        public class ChatCommandDefinition
        {
            public string Description { get; }
            public CommandPermissionType Permissions { get; }
            public ExecuteChatCommandDelegate DoAction { get; }
            public ValidateArgumentsDelegate ValidateArguments { get; }
            public bool RequiresTarget { get; }

            public ChatCommandDefinition(
                string description, 
                CommandPermissionType permissions, 
                ExecuteChatCommandDelegate doAction, 
                ValidateArgumentsDelegate validateArguments, 
                bool requiresTarget)
            {
                Description = description;
                Permissions = permissions;
                DoAction = doAction;
                ValidateArguments = validateArguments;
                RequiresTarget = requiresTarget;
            }
        }

        private static Dictionary<string, ChatCommandDefinition> ChatCommands { get; set; } = new Dictionary<string, ChatCommandDefinition>();
        private static string HelpTextPlayer { get; set; }
        private static string HelpTextDM { get; set; }
        private static string HelpTextAdmin { get; set; }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            Console.WriteLine("Loading chat commands");
            LoadChatCommands();
            BuildHelpText();
        }

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        [NWNEventHandler("on_nwnx_chat")]
        public static void HandleChatMessage()
        {
            uint sender = OBJECT_SELF;
            string originalMessage = Chat.GetMessage().Trim();

            if (!CanHandleChat(sender, originalMessage))
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

            Chat.SkipMessage();

            if (!ChatCommands.ContainsKey(command))
            {
                SendMessageToPC(sender, ColorToken.Red("Invalid chat command. Use '/help' to get a list of available commands."));
                return;
            }

            var chatCommand = ChatCommands[command];
            
            string args = string.Join(" ", split);

            if (!chatCommand.RequiresTarget)
            {
                ProcessChatCommand(command, sender, OBJECT_INVALID, null, args);
            }
            else
            {
                string error = chatCommand.ValidateArguments(sender, split.ToArray());
                if (!string.IsNullOrWhiteSpace(error))
                {
                    SendMessageToPC(sender, error);
                    return;
                }

                SetLocalString(sender, "CHAT_COMMAND", command);
                SetLocalString(sender, "CHAT_COMMAND_ARGS", command);
                SendMessageToPC(sender, "Please use your 'Chat Command Targeter' feat to select the target of this chat command.");

                if (!GetHasFeat(Feat.ChatCommandTargeter, sender) || GetIsDM(sender) || GetIsDMPossessed(sender))
                {
                    Creature.AddFeatByLevel(sender, Feat.ChatCommandTargeter, 1);

                    if (GetIsDM(sender) || GetIsDMPossessed(sender))
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

        /// <summary>
        /// Parse the message and ensure it starts with a slash.
        /// Sender must be a player or DM.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message sent.</param>
        /// <returns>true if this is a chat command, false otherwise</returns>
        private static bool CanHandleChat(uint sender, string message)
        {
            bool validTarget = GetIsPC(sender) || GetIsDM(sender) || GetIsDMPossessed(sender);
            bool validMessage = message.Length >= 2 && message[0] == '/' && message[1] != '/';
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
            var command = ChatCommands[commandName];
            if (targetLocation == null)
            {
                targetLocation = new Location(IntPtr.Zero);
            }

            var authorization = Authorization.GetAuthorizationLevel(sender);

            if (command.Permissions.HasFlag(CommandPermissionType.Player) && authorization == AuthorizationLevel.Player ||
                 command.Permissions.HasFlag(CommandPermissionType.DM) && authorization == AuthorizationLevel.DM ||
                 command.Permissions.HasFlag(CommandPermissionType.Admin) && authorization == AuthorizationLevel.Admin)
            {
                string[] argsArr = string.IsNullOrWhiteSpace(args) ? new string[0] : args.Split(' ').ToArray();
                string error = command.ValidateArguments(sender, argsArr);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    SendMessageToPC(sender, error);
                }
                else
                {
                    command.DoAction(sender, target, targetLocation, argsArr);
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
            ChatCommands["bored"] = new ChatCommandDefinition(
                "Plays a bored animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetPauseBored));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["bow"] = new ChatCommandDefinition(
                "Plays a bow animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetBow));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["bug"] = new BugReportChatCommandDefinition();

            ChatCommands["cdkey"] = new ChatCommandDefinition(
                "Displays your public CD key.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    string cdKey = GetPCPublicCDKey(user);
                    SendMessageToPC(user, "Your public CD Key is: " + cdKey);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["coord"] = new ChatCommandDefinition(
                "Displays your current coordinates in the area.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    Vector3 position = GetPosition(user);
                    int cellX = (int)(position.X / 10);
                    int cellY = (int)(position.Y / 10);

                    SendMessageToPC(user, $"Current Area Coordinates: ({cellX}, {cellY})");
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["copyitem"] = new ChatCommandDefinition(
                "Copies the targeted item.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    if (GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "You can only copy items with this command.");
                        return;
                    }

                    CopyItem(target, user, true);
                    SendMessageToPC(user, "Item copied successfully.");
                },
                (user, args) => string.Empty,
                true);

            ChatCommands["day"] = new ChatCommandDefinition(
                "Sets the world time to 8 AM.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    SetTime(8, 0, 0, 0);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["deadback"] = new ChatCommandDefinition(
                "Plays a dead back animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingDeadBack, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["deadfront"] = new ChatCommandDefinition(
                "Plays a dead front animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingDeadFront, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["delete"] = new CharacterDeletionChatCommandDefinition();

            ChatCommands["dice"] = new DiceChatCommandDefinition();


            ChatCommands["drink"] = new ChatCommandDefinition(
                "Plays a drinking animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetDrink));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["drunk"] = new ChatCommandDefinition(
                "Plays a drunk animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingPauseDrunk, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["duck"] = new ChatCommandDefinition(
                "Plays a duck animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetDodgeDuck));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["getlocalfloat"] = new GetLocalFloatChatCommandDefinition();
            ChatCommands["getlocalint"] = new GetLocalIntChatCommandDefinition();
            ChatCommands["getlocalstring"] = new GetLocalStringChatCommandDefinition();

            ChatCommands["getplot"] = new ChatCommandDefinition(
                "Gets whether an object is marked plot.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    if (GetPlotFlag(target))
                    {
                        SendMessageToPC(user, "Target is marked plot.");
                    }
                    else
                    {
                        SendMessageToPC(user, "Target is NOT marked plot.");
                    }
                },
                (user, args) => string.Empty,
                true);

            ChatCommands["greet"] = new ChatCommandDefinition(
                "Plays a greet animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetGreeting));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["help"] = new ChatCommandDefinition(
                "Displays all chat commands available to you.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    var authorization = Authorization.GetAuthorizationLevel(user);

                    if (authorization == AuthorizationLevel.DM)
                    {
                        SendMessageToPC(user, HelpTextDM);
                    }
                    else if (authorization == AuthorizationLevel.Admin)
                    {
                        SendMessageToPC(user, HelpTextAdmin);
                    }
                    else
                    {
                        SendMessageToPC(user, HelpTextPlayer);
                    }
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["interact"] = new ChatCommandDefinition(
                "Plays an interact animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.LoopingGetMid));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["kill"] = new ChatCommandDefinition(
                "Kills your target.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    var amount = GetMaxHitPoints(target) + 11;
                    var damage = EffectDamage(amount);
                    ApplyEffectToObject(DurationType.Instant, damage, target);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["laughing"] = new ChatCommandDefinition(
                "Plays a laughing animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingTalkLaughing, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["listen"] = new ChatCommandDefinition(
                "Plays a listen animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingListen, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["look"] = new ChatCommandDefinition(
                "Plays a look far animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingLookFar, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["name"] = new ChatCommandDefinition(
                "Plays a drunk animation.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    if (GetIsPC(target) || GetIsDM(target))
                    {
                        SendMessageToPC(user, "PCs cannot be targeted with this command.");
                        return;
                    }

                    string name = string.Empty;
                    foreach (var arg in args)
                    {
                        name += " " + arg;
                    }

                    SetName(target, name);
                },
                (user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Please enter a name. Example: /name My Creature";
                    }

                    return string.Empty;
                },
                false);

            ChatCommands["night"] = new ChatCommandDefinition(
                "Sets the world time to 8 AM.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    SetTime(20, 0, 0, 0);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["pickup"] = new ChatCommandDefinition(
                "Plays an interact animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.LoopingGetLow));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["pos"] = new ChatCommandDefinition(
                "Displays your current position in the area.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    var position = GetPosition(user);
                    SendMessageToPC(user, $"Current Position: ({position.X}, {position.Y}, {position.Z})");
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["read"] = new ChatCommandDefinition(
                "Plays a read animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetRead));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["rest"] = new ChatCommandDefinition(
                "Opens the rest menu.",
                CommandPermissionType.Player,
                (user, target, location, args) =>
                {
                    Dialog.StartConversation(user, user, nameof(RestMenu));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["rez"] = new ChatCommandDefinition(
                "Revives you, heals you to full, and restores all FP.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    if (GetIsDead(user))
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), user);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(999), user);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["salute"] = new ChatCommandDefinition(
                "Plays a salute animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetSalute));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["save"] = new ChatCommandDefinition(
                "Manually saves your character. Your character also saves automatically every few minutes.",
                CommandPermissionType.Player,
                (user, target, location, args) =>
                {
                    ExportSingleCharacter(user);
                    SendMessageToPC(user, "Character saved successfully.");
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["scratchhead"] = new ChatCommandDefinition(
                "Plays a scratch head animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetPauseScratchHead));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["setlocalfloat"] = new SetLocalFloatChatCommandDefinition();
            ChatCommands["setlocalint"] = new SetLocalIntChatCommandDefinition();
            ChatCommands["setlocalstring"] = new SetLocalStringChatCommandDefinition();

            ChatCommands["setportrait"] = new SetPortraitChatCommandDefinition();

            ChatCommands["sidestep"] = new ChatCommandDefinition(
                "Plays a side-step animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetDodgeSide));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["sit"] = new ChatCommandDefinition(
                "Makes your character sit down.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingSitCross, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["spasm"] = new ChatCommandDefinition(
                "Plays a spasm animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetSpasm));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["spawngold"] = new ChatCommandDefinition(
                "Spawns gold of a specific quantity on your character. Example: /spawngold 33",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    int quantity = 1;

                    if (args.Length >= 1)
                    {
                        if (!int.TryParse(args[0], out quantity))
                        {
                            return;
                        }
                    }

                    GiveGoldToCreature(user, quantity);
                },
                (user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a quantity. Example: /spawngold 34");
                    }
                    return string.Empty;
                },
                false);

            ChatCommands["spawnitem"] = new SpawnItemChatCommandDefinition();

            ChatCommands["taunt"] = new ChatCommandDefinition(
                "Plays a taunt animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetTaunt));
                },
                (user, args) => string.Empty,
                false);


            ChatCommands["time"] = new ChatCommandDefinition(
                "Returns the current UTC server time.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    DateTime now = DateTime.UtcNow;
                    string nowText = now.ToString("yyyy-MM-dd hh:mm:ss");
                    
                    SendMessageToPC(user, "Current Server Date: " + nowText);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["tired"] = new ChatCommandDefinition(
                "Plays a taunt animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    LoopingAnimationAction(user, Animation.LoopingPauseTired, args);
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["tpwp"] = new ChatCommandDefinition(
                "Teleports you to a waypoint with a specified tag.",
                CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    string tag = args[0];
                    uint wp = GetWaypointByTag(tag);

                    if (!GetIsObjectValid(wp))
                    {
                        SendMessageToPC(user, "Invalid waypoint tag. Did you enter the right tag?");
                        return;
                    }

                    AssignCommand(user, () => ActionJumpToLocation(GetLocation(wp)));
                },
                (user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "You must specify a waypoint tag. Example: /tpwp MY_WAYPOINT_TAG";
                    }

                    return string.Empty;
                },
                false);

            ChatCommands["victory1"] = new ChatCommandDefinition(
                "Plays a victory 1 animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetVictory1));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["victory2"] = new ChatCommandDefinition(
                "Plays a victory 2 animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetVictory2));
                },
                (user, args) => string.Empty,
                false);

            ChatCommands["victory3"] = new ChatCommandDefinition(
                "Plays a victory 3 animation.",
                CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin,
                (user, target, location, args) =>
                {
                    AssignCommand(user, () => ActionPlayAnimation(Animation.FireForgetVictory3));
                },
                (user, args) => string.Empty,
                false);
        }

        /// <summary>
        /// Plays a looping animation action.
        /// </summary>
        /// <param name="user">The sender of the command.</param>
        /// <param name="animation">The animation to play.</param>
        /// <param name="args">The args sent by the user.</param>
        private static void LoopingAnimationAction(uint user, Animation animation, params string[] args)
        {
            float duration = 9999.0f;

            if (args.Length > 0)
            {
                if (!float.TryParse(args[0], out duration))
                {
                    duration = 9999.0f;
                }
            }

            AssignCommand(user, () => ActionPlayAnimation(animation, 1.0f, duration));
        }

        /// <summary>
        /// Builds text used by the /help command for each authorization level.
        /// This must be called after LoadChatCommands or there will be nothing to process.
        /// </summary>
        private static void BuildHelpText()
        {
            foreach(var command in ChatCommands)
            {
                var text = command.Key;
                var definition = command.Value;

                if (definition.Permissions.HasFlag(CommandPermissionType.Player))
                {
                    HelpTextPlayer += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }

                if (definition.Permissions.HasFlag(CommandPermissionType.Player))
                {
                    HelpTextDM += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }

                if (definition.Permissions.HasFlag(CommandPermissionType.Player))
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
            var feat = (Feat)Convert.ToInt32(Events.GetEventData("FEAT_ID"));
            if (feat != Feat.ChatCommandTargeter) return;

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
