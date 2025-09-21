using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Game.Server.Service
{
    public interface IChatCommandService
    {
        /// <summary>
        /// Gets the help text for players
        /// </summary>
        string HelpTextPlayer { get; }

        /// <summary>
        /// Gets the help text for emotes
        /// </summary>
        string HelpTextEmote { get; }

        /// <summary>
        /// Gets the help text for DMs
        /// </summary>
        string HelpTextDM { get; }

        /// <summary>
        /// Gets the help text for admins
        /// </summary>
        string HelpTextAdmin { get; }

        /// <summary>
        /// Gets the list of emote names
        /// </summary>
        GuiBindingList<string> EmoteNames { get; }

        /// <summary>
        /// Gets the list of emote descriptions
        /// </summary>
        GuiBindingList<string> EmoteDescriptions { get; }

        /// <summary>
        /// Gets the list of emote animations
        /// </summary>
        List<Animation> EmoteAnimations { get; }

        /// <summary>
        /// Gets the list of emote looping flags
        /// </summary>
        GuiBindingList<bool> EmoteIsLooping { get; }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        void OnModuleLoad();

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        /// <param name="sender">The creature who sent the command</param>
        /// <param name="message">The message content</param>
        /// <returns>true if the command was processed, false otherwise</returns>
        bool ProcessChatCommand(uint sender, string message);

        /// <summary>
        /// Gets all available chat commands for a given user type
        /// </summary>
        /// <param name="userType">The type of user (Player, DM, Admin)</param>
        /// <returns>A dictionary of command names to command details</returns>
        Dictionary<string, ChatCommandDetail> GetChatCommands(ChatCommandUserType userType);

        /// <summary>
        /// Gets all available emote commands
        /// </summary>
        /// <returns>A dictionary of emote names to command details</returns>
        Dictionary<string, ChatCommandDetail> GetEmoteCommands();
    }
}
