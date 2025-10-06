using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Communication.EventHandlers
{
    /// <summary>
    /// Event handlers for Communication-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class CommunicationEventHandlers
    {
        private readonly IServiceProvider _serviceProvider;

        // Lazy-loaded services to break circular dependencies
        private IChatCommandService ChatCommandService => _serviceProvider.GetRequiredService<IChatCommandService>();
        private ICommunicationService CommunicationService => _serviceProvider.GetRequiredService<ICommunicationService>();
        private ILanguageService LanguageService => _serviceProvider.GetRequiredService<ILanguageService>();
        private IHoloComService HoloComService => _serviceProvider.GetRequiredService<IHoloComService>();
        private IRoleplayXPService RoleplayXP => _serviceProvider.GetRequiredService<IRoleplayXPService>();

        public CommunicationEventHandlers(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => OnModuleLoad());
            eventAggregator.Subscribe<OnNWNXChat>(e => OnNWNXChat());
            eventAggregator.Subscribe<OnDMPossessBefore>(e => OnDMPossess());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnter());
            eventAggregator.Subscribe<OnNWNXChat>(e => TypingIndicator());
            eventAggregator.Subscribe<OnModuleChat>(e => OnModuleChat());
            eventAggregator.Subscribe<OnModuleLoad>(e => LoadTranslators());
            eventAggregator.Subscribe<OnPlayerHeartbeat>(e => DistributeRoleplayXP());
            eventAggregator.Subscribe<OnModuleDeath>(e => OnModuleDeath());
            eventAggregator.Subscribe<OnModuleExit>(e => OnModuleLeave());
        }

        /// <summary>
        /// Loads all chat commands into cache and builds the related help text.
        /// </summary>
        public void OnModuleLoad()
        {
            ChatCommandService.LoadChatCommands();
            ChatCommandService.BuildHelpText();
            ChatCommandService.BuildEmoteUILists();
        }

        /// <summary>
        /// Handle all NWNX chat events for communication features.
        /// </summary>
        public void OnNWNXChat()
        {
            ChatCommandService.HandleChatMessage();
            CommunicationService.ProcessChatMessage();
            RoleplayXP.ProcessRPMessage();
        }

        /// <summary>
        /// Whenever a DM possesses a creature, track the NPC on their object so that messages can be
        /// sent to them during the possession.
        /// </summary>

        public void OnDMPossess()
        {
            CommunicationService.OnDMPossess();
        }

        /// <summary>
        /// When a player enters the server, handle all communication-related initialization.
        /// </summary>
        public void OnModuleEnter()
        {
            CommunicationService.LoadHolonetSetting();
            HoloComService.OnModuleEnter();
        }

        /// <summary>
        /// When a player is typing in a chat window, show an indicator to other players.
        /// When they stop typing or the window becomes unfocused, remove the indicator.
        /// </summary>
        public void TypingIndicator()
        {
            CommunicationService.TypingIndicator();
        }

        /// <summary>
        /// Handle all chat-related events for communication features.
        /// </summary>
        public void OnModuleChat()
        {
            CommunicationService.ProcessNativeChatMessage();
            HoloComService.OnModuleChat();
        }


        /// <summary>
        /// When the module loads, create translators for every language and store them into cache.
        /// </summary>
        public void LoadTranslators()
        {
            LanguageService.LoadTranslators();
            CommunicationService.ConfigureFeedbackMessages();
        }

        /// <summary>
        /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
        /// </summary>
        public void DistributeRoleplayXP()
        {
            RoleplayXP.DistributeRoleplayXP();
        }


        /// <summary>
        /// Handle player death in HoloCom calls.
        /// </summary>
        public void OnModuleDeath()
        {
            HoloComService.OnModuleDeath();
        }


        /// <summary>
        /// Handle player leaving the server for HoloCom.
        /// </summary>
        public void OnModuleLeave()
        {
            HoloComService.OnModuleLeave();
        }

    }
}
