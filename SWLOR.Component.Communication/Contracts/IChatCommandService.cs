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
        List<Animation> EmoteAnimations { get; }
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
