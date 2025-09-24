using SWLOR.Component.Communication.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Communication.Service;

public interface IChatCommandBuilder
{
    /// <summary>
    /// Creates a new chat command with the given name(s).
    /// </summary>
    /// <param name="command">The primary command name.</param>
    /// <param name="alternativeCommands">Alternative commands which also perform the same method.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder Create(string command, params string[] alternativeCommands);

    /// <summary>
    /// Sets the description of the active chat command.
    /// </summary>
    /// <param name="description">The description to set.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder Description(string description);

    /// <summary>
    /// Sets the permissions which are permitted to use the chat command.
    /// </summary>
    /// <param name="authorizationLevels">The authorization levels to allow.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder Permissions(params AuthorizationLevel[] authorizationLevels);

    /// <summary>
    /// Defines a validation routine for arguments. This runs before the action procedure.
    /// </summary>
    /// <param name="validation">The validation routine to run against arguments.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder Validate(ChatCommandDetail.ValidateArgumentsDelegate validation);

    /// <summary>
    /// Defines an action routine for the command. This runs after the validation procedure.
    /// </summary>
    /// <param name="action">The action routine to run.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder Action(ChatCommandDetail.ExecuteChatCommandDelegate action);

    /// <summary>
    /// Indicates the chat command is an emote and should be categorized under that instead of
    /// the general purpose chat commands.
    /// </summary>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder IsEmote();

    /// <summary>
    /// Sets the action to play an animation.
    /// </summary>
    /// <param name="animation">The animation to play.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder AnimationAction(Animation animation);

    /// <summary>
    /// Sets the action to play a looping animation.
    /// </summary>
    /// <param name="animation">The looping animation to play.</param>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder AnimationLoopingAction(Animation animation);

    /// <summary>
    /// If specified, this command requires a target to run.
    /// The objectTypes argument determines the type of objects that can be selected.
    /// </summary>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder RequiresTarget(ObjectType objectTypes = ObjectType.All);

    /// <summary>
    /// Indicates this chat command can be used by anyone if the server is set to 'test' mode.
    /// </summary>
    /// <returns>A configured ChatCommandBuilder.</returns>
    IChatCommandBuilder AvailableToAllOnTestEnvironment();

    /// <summary>
    /// Builds all of the chat commands constructed by this builder.
    /// </summary>
    /// <returns>A dictionary containing all chat command configurations.</returns>
    Dictionary<string, ChatCommandDetail> Build();
}