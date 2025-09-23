using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Feature;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Communication.EventHandlers
{
    /// <summary>
    /// Event handlers for Communication-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class CommunicationEventHandlers
    {
        private readonly IChatCommandService _chatCommandService;
        private readonly ICommunicationService _communicationService;
        private readonly ILanguageService _languageService;
        private readonly IHoloComService _holoComService;
        private readonly IRoleplayXPService _roleplayXP;

        public CommunicationEventHandlers(
            IChatCommandService chatCommandService,
            ICommunicationService communicationService,
            ILanguageService languageService,
            IHoloComService holoComService,
            IRoleplayXPService roleplayXP)
        {
            _chatCommandService = chatCommandService;
            _communicationService = communicationService;
            _languageService = languageService;
            _holoComService = holoComService;
            _roleplayXP = roleplayXP;
        }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void OnModuleLoad()
        {
            _chatCommandService.LoadChatCommands();
            _chatCommandService.BuildHelpText();
            _chatCommandService.BuildEmoteUILists();
        }

        /// <summary>
        /// Handle all NWNX chat events for communication features.
        /// </summary>
        [ScriptHandler<OnNWNXChat>]
        public void OnNWNXChat()
        {
            _chatCommandService.HandleChatMessage();
            _communicationService.ProcessChatMessage();
            _roleplayXP.ProcessRPMessage();
        }

        /// <summary>
        /// Whenever a DM possesses a creature, track the NPC on their object so that messages can be
        /// sent to them during the possession.
        /// </summary>
        [ScriptHandler<OnDMPossessBefore>]
        [ScriptHandler<OnDMPossessFullPowerBefore>]
        public void OnDMPossess()
        {
            _communicationService.OnDMPossess();
        }

        /// <summary>
        /// When a player enters the server, handle all communication-related initialization.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void OnModuleEnter()
        {
            _communicationService.LoadHolonetSetting();
            _holoComService.OnModuleEnter();
        }

        /// <summary>
        /// When a player is typing in a chat window, show an indicator to other players.
        /// When they stop typing or the window becomes unfocused, remove the indicator.
        /// </summary>
        [ScriptHandler<OnModuleGuiEvent>]
        public void TypingIndicator()
        {
            _communicationService.TypingIndicator();
        }

        /// <summary>
        /// Handle all chat-related events for communication features.
        /// </summary>
        [ScriptHandler<OnModuleChat>]
        public void OnModuleChat()
        {
            _communicationService.ProcessNativeChatMessage();
            _holoComService.OnModuleChat();
        }


        /// <summary>
        /// When the module loads, create translators for every language and store them into cache.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadTranslators()
        {
            _languageService.LoadTranslators();
            _communicationService.ConfigureFeedbackMessages();
        }

        /// <summary>
        /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
        /// </summary>
        [ScriptHandler<OnPlayerHeartbeat>]
        public void DistributeRoleplayXP()
        {
            _roleplayXP.DistributeRoleplayXP();
        }


        /// <summary>
        /// Handle player death in HoloCom calls.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void OnModuleDeath()
        {
            _holoComService.OnModuleDeath();
        }


        /// <summary>
        /// Handle player leaving the server for HoloCom.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void OnModuleLeave()
        {
            _holoComService.OnModuleLeave();
        }

    }
}
