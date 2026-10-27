using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Chat commands: loads the command definitions, builds the help text and the emote lists, and runs the commands
    /// players and DMs type. The static facade <c>Service.ChatCommand</c> forwards to the container's instance of this
    /// interface and keeps the event handlers. The definitions in <c>Feature/ChatCommandDefinition</c> keep calling the
    /// facade.
    /// </summary>
    public interface IChatCommandService
    {
        /// <summary>The /help text for players.</summary>
        string HelpTextPlayer { get; }

        /// <summary>The /help text for emotes.</summary>
        string HelpTextEmote { get; }

        /// <summary>The /help text for DMs.</summary>
        string HelpTextDM { get; }

        /// <summary>The /help text for admins.</summary>
        string HelpTextAdmin { get; }

        /// <summary>The names of the emote commands, in the order the emotes window lists them.</summary>
        GuiBindingList<string> EmoteNames { get; }

        /// <summary>The descriptions of the emote commands, parallel to <see cref="EmoteNames"/>.</summary>
        GuiBindingList<string> EmoteDescriptions { get; }

        /// <summary>The animations of the emote commands, parallel to <see cref="EmoteNames"/>.</summary>
        List<Animation> EmoteAnimations { get; }

        /// <summary>Whether each emote command loops, parallel to <see cref="EmoteNames"/>.</summary>
        GuiBindingList<bool> EmoteIsLooping { get; }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        void OnModuleLoad();

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        void HandleChatMessage();
    }
}
