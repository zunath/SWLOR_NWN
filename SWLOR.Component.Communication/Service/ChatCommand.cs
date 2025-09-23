using SWLOR.Component.Admin.Service;
using SWLOR.Component.Communication.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using ChatCommandDetail = SWLOR.Component.Communication.Model.ChatCommandDetail;

namespace SWLOR.Component.Communication.Service
{
    public class ChatCommand : IChatCommandService
    {
        private readonly IAppSettings _appSettings;
        private readonly IAuthorizationService _authorization;
        private readonly ITargetingService _targetingService;

        private readonly Dictionary<string, ChatCommandDetail> _chatCommands = new();
        private readonly Dictionary<string, ChatCommandDetail> _emoteCommands = new();
        public string HelpTextPlayer { get; private set; }
        public string HelpTextEmote { get; private set; }
        public string HelpTextDM { get; private set; }
        public string HelpTextAdmin { get; private set; }

        public GuiBindingList<string> EmoteNames { get; } = new();
        public GuiBindingList<string> EmoteDescriptions { get; } = new();
        public List<Animation> EmoteAnimations { get; } = new();
        public GuiBindingList<bool> EmoteIsLooping { get; } = new();

        public ChatCommand(IAppSettings appSettings, IAuthorizationService authorization, ITargetingService targetingService)
        {
            _appSettings = appSettings;
            _authorization = authorization;
            _targetingService = targetingService;
        }

        private const string InvalidChatCommandMessage = "Invalid chat command. Use '/help' to get a list of available commands.";


        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        public void HandleChatMessage()
        {
            var sender = OBJECT_SELF;
            var originalMessage = ChatPlugin.GetMessage().Trim();

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

            ChatPlugin.SkipMessage();

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


                var authorization = _authorization.GetAuthorizationLevel(sender);

                if ((_appSettings.ServerEnvironment == ServerEnvironmentType.Test && chatCommand.AvailableToAllOnTestEnvironment) ||
                    chatCommand.Authorization.HasFlag(authorization))
                {
                    _targetingService.EnterTargetingMode(sender, chatCommand.ValidTargetTypes, "Please click on a target for this chat command.",
                    target =>
                    {
                        var location = GetIsObjectValid(target)
                            ? GetLocation(target)
                            : Location(GetArea(sender), GetTargetingModeSelectedPosition(), 0.0f);
                        ProcessChatCommand(command, sender, target, location, args);
                    });
                }
                else
                {
                    SendMessageToPC(sender, ColorToken.Red(InvalidChatCommandMessage));
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
        private bool CanHandleChat(uint sender, string message)
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
        private void ProcessChatCommand(string commandName, uint sender, uint target, Location targetLocation, string args)
        {
            var command = _chatCommands[commandName];
            if (targetLocation == null)
            {
                targetLocation = new Location(IntPtr.Zero);
            }

            var authorization = _authorization.GetAuthorizationLevel(sender);

            if ((_appSettings.ServerEnvironment == ServerEnvironmentType.Test && command.AvailableToAllOnTestEnvironment) ||
                command.Authorization.HasFlag(authorization))
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
                SendMessageToPC(sender, ColorToken.Red(InvalidChatCommandMessage));
            }
        }

        /// <summary>
        /// Builds all chat commands and puts them into cache.
        /// </summary>
        public void LoadChatCommands()
        {
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

                    if (value.IsEmote)
                    {
                        _emoteCommands[key] = value;
                    }
                }
            }

            Console.WriteLine($"Loaded {_chatCommands.Count} chat commands.");
        }

        /// <summary>
        /// Builds text used by the /help command for each authorization level.
        /// This must be called after LoadChatCommands or there will be nothing to process.
        /// </summary>
        public void BuildHelpText()
        {
            var orderedCommands = _chatCommands.OrderBy(o => o.Key);

            foreach (var command in orderedCommands)
            {
                var text = command.Key;
                var definition = command.Value;

                if (definition.Authorization.HasFlag(AuthorizationLevel.Player))
                {
                    if (definition.IsEmote)
                    {
                        HelpTextEmote += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                    }
                    else
                    {
                        HelpTextPlayer += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                    }
                }

                if (definition.Authorization.HasFlag(AuthorizationLevel.DM))
                {
                    if(!definition.IsEmote)
                        HelpTextDM += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }

                if (definition.Authorization.HasFlag(AuthorizationLevel.Admin))
                {
                    if (!definition.IsEmote)
                        HelpTextAdmin += ColorToken.Green("/" + text) + ColorToken.White(": " + definition.Description) + "\n";
                }
            }
        }

        public void BuildEmoteUILists()
        {
            foreach (var (text, command) in _emoteCommands)
            {
                EmoteNames.Add(text);
                EmoteDescriptions.Add(command.Description);
                EmoteAnimations.Add(command.EmoteAnimation);
                EmoteIsLooping.Add(command.IsEmoteLooping);
            }
        }

    }
}
