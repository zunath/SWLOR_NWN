using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Communication.Contracts
{

    public interface IChatCommandService
    {
        string HelpTextPlayer { get; }
        string HelpTextEmote { get; }
        string HelpTextDM { get; }
        string HelpTextAdmin { get; }
        GuiBindingList<string> EmoteNames { get; }
        GuiBindingList<string> EmoteDescriptions { get; }
        List<AnimationType> EmoteAnimations { get; }
        GuiBindingList<bool> EmoteIsLooping { get; }

        /// <summary>
        /// Handles validating and processing chat commands sent by players and DMs.
        /// </summary>
        void HandleChatMessage();

        /// <summary>
        /// Builds all chat commands and puts them into cache.
        /// </summary>
        void LoadChatCommands();

        /// <summary>
        /// Builds text used by the /help command for each authorization level.
        /// This must be called after LoadChatCommands or there will be nothing to process.
        /// </summary>
        void BuildHelpText();

        void BuildEmoteUILists();
    }
}
