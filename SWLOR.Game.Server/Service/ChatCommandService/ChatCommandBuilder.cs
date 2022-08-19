using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.ChatCommandService
{
    public class ChatCommandBuilder
    {
        private readonly Dictionary<string, ChatCommandDetail> _chatCommands = new Dictionary<string, ChatCommandDetail>();
        private ChatCommandDetail _currentDetail;

        /// <summary>
        /// Creates a new chat command with the given name(s).
        /// </summary>
        /// <param name="command">The primary command name.</param>
        /// <param name="alternativeCommands">Alternative commands which also perform the same method.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder Create(string command, params string[] alternativeCommands)
        {
            _currentDetail = new ChatCommandDetail();
            _chatCommands[command] = _currentDetail;

            if (alternativeCommands != null)
            {
                foreach (var alternativeCommand in alternativeCommands)
                {
                    _chatCommands[alternativeCommand] = _currentDetail;
                }
            }

            return this;
        }

        /// <summary>
        /// Sets the description of the active chat command.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder Description(string description)
        {
            _currentDetail.Description = description;

            return this;
        }

        /// <summary>
        /// Sets the permissions which are permitted to use the chat command.
        /// </summary>
        /// <param name="authorizationLevels">The authorization levels to allow.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder Permissions(params AuthorizationLevel[] authorizationLevels)
        {
            var authorization = AuthorizationLevel.None;

            foreach (var authorizationLevel in authorizationLevels)
            {
                authorization |= authorizationLevel;
            }

            _currentDetail.Authorization = authorization;

            return this;
        }

        /// <summary>
        /// Defines a validation routine for arguments. This runs before the action procedure.
        /// </summary>
        /// <param name="validation">The validation routine to run against arguments.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder Validate(ChatCommandDetail.ValidateArgumentsDelegate validation)
        {
            _currentDetail.ValidateArguments = validation;

            return this;
        }

        /// <summary>
        /// Defines an action routine for the command. This runs after the validation procedure.
        /// </summary>
        /// <param name="action">The action routine to run.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder Action(ChatCommandDetail.ExecuteChatCommandDelegate action)
        {
            _currentDetail.DoAction = action;

            return this;
        }
        /// <summary>
        /// Indicates the chat command is an emote and should be categorized under that instead of
        /// the general purpose chat commands.
        /// </summary>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder IsEmote()
        {
            _currentDetail.IsEmote = true;

            return this;
        }
        /// <summary>
        /// Sets the action to play an animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder AnimationAction(Animation animation)
        {
            _currentDetail.DoAction = (user, target, location, args) =>
            {
                AssignCommand(user, () => ActionPlayAnimation(animation));
            };
            _currentDetail.EmoteAnimation = animation;
            _currentDetail.IsEmoteLooping = false;

            return this;
        }

        /// <summary>
        /// Sets the action to play a looping animation.
        /// </summary>
        /// <param name="animation">The looping animation to play.</param>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder AnimationLoopingAction(Animation animation)
        {
            _currentDetail.DoAction = (user, target, location, args) =>
            {
                var duration = 9999.9f;

                if (args.Length > 0)
                {
                    if (!float.TryParse(args[0], out duration))
                    {
                        duration = 9999.9f;
                    }
                }

                AssignCommand(user, () => ActionPlayAnimation(animation, 1f, duration));
            };
            _currentDetail.EmoteAnimation = animation;
            _currentDetail.IsEmoteLooping = true;

            return this;
        }

        /// <summary>
        /// If specified, this command requires a target to run.
        /// The objectTypes argument determines the type of objects that can be selected.
        /// </summary>
        /// <returns>A configured ChatCommandBuilder.</returns>
        public ChatCommandBuilder RequiresTarget(ObjectType objectTypes = ObjectType.All)
        {
            _currentDetail.RequiresTarget = true;
            _currentDetail.ValidTargetTypes = objectTypes;

            return this;
        }

        /// <summary>
        /// Builds all of the chat commands constructed by this builder.
        /// </summary>
        /// <returns>A dictionary containing all chat command configurations.</returns>
        public Dictionary<string, ChatCommandDetail> Build()
        {
            return _chatCommands;
        }
    }
}
