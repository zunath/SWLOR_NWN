using SWLOR.Component.Admin.Contracts;
using SWLOR.Component.Admin.Feature;
using SWLOR.Component.Admin.UI.ViewModel;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Server;

namespace SWLOR.Component.Admin.EventHandlers
{
    /// <summary>
    /// Event handlers for Admin-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class AdminEventHandlers
    {
        private readonly TlkOverrides _tlkOverrides;
        private readonly DMActions _dmActions;
        private readonly DMPlayerExamineViewModel _dmPlayerExamineViewModel;
        private readonly ServerTasks _serverTasks;
        private readonly DebugEnmityViewModel _debugEnmityViewModel;
        private readonly CreatureManagerViewModel _creatureManagerViewModel;
        private readonly DMAuthorization _dmAuthorization;
        private readonly IAuditingService _auditingService;

        public AdminEventHandlers(
            TlkOverrides tlkOverrides,
            DMActions dmActions,
            DMPlayerExamineViewModel dmPlayerExamineViewModel,
            ServerTasks serverTasks,
            DebugEnmityViewModel debugEnmityViewModel,
            CreatureManagerViewModel creatureManagerViewModel,
            DMAuthorization dmAuthorization,
            IAuditingService auditingService,
            IEventAggregator eventAggregator)
        {
            _tlkOverrides = tlkOverrides;
            _dmActions = dmActions;
            _dmPlayerExamineViewModel = dmPlayerExamineViewModel;
            _serverTasks = serverTasks;
            _debugEnmityViewModel = debugEnmityViewModel;
            _creatureManagerViewModel = creatureManagerViewModel;
            _dmAuthorization = dmAuthorization;
            _auditingService = auditingService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => OverrideTlks());
            eventAggregator.Subscribe<OnModuleLoad>(e => ProcessBootUp());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnter());
            eventAggregator.Subscribe<OnModuleExit>(e => OnModuleExit());
            eventAggregator.Subscribe<OnDMSpawnObjectAfter>(e => OnDMSpawnObject());
            eventAggregator.Subscribe<OnDMGiveXPBefore>(e => GrantRPXPViaDMCommand());
            eventAggregator.Subscribe<OnDMGiveLevelBefore>(e => DisableGiveLevel());
            eventAggregator.Subscribe<OnExamineObjectBefore>(e => ExaminePlayer());
            eventAggregator.Subscribe<OnServerHeartbeat>(e => ProcessAutoRestart());
            eventAggregator.Subscribe<OnEnmityChanged>(e => OnEnmityChanged());
            eventAggregator.Subscribe<OnModulePlayerTarget>(e => RunTargetedLocationAction());
            eventAggregator.Subscribe<OnNWNXChat>(e => AuditChatMessages());
        }

        /// <summary>
        /// When the module loads, override TLK strings.
        /// </summary>
        public void OverrideTlks()
        {
            _tlkOverrides.OverrideTlks();
        }

        /// <summary>
        /// When the server starts up, process boot up tasks.
        /// </summary>
        public void ProcessBootUp()
        {
            _serverTasks.ProcessBootUp();
        }

        /// <summary>
        /// When a player enters the server, handle all admin-related initialization.
        /// </summary>
        public void OnModuleEnter()
        {
            _serverTasks.WelcomeMessage();
            _dmAuthorization.VerifyDM();
            _auditingService.AuditClientConnection();
        }

        /// <summary>
        /// When a player exits the server, audit the disconnection.
        /// </summary>
        public void OnModuleExit()
        {
            _auditingService.AuditClientDisconnection();
        }

        /// <summary>
        /// When a DM spawns an object, handle the spawn event.
        /// </summary>
        public void OnDMSpawnObject()
        {
            _dmActions.OnDMSpawnObject();
        }

        /// <summary>
        /// When a DM gives XP, handle RP XP granting.
        /// </summary>
        public void GrantRPXPViaDMCommand()
        {
            _dmActions.GrantRPXPViaDMCommand();
        }

        /// <summary>
        /// When a DM tries to give levels, disable the command.
        /// </summary>
        public void DisableGiveLevel()
        {
            _dmActions.DisableGiveLevel();
        }

        /// <summary>
        /// When a player examines an object, handle DM player examination.
        /// </summary>
        public void ExaminePlayer()
        {
            _dmPlayerExamineViewModel.ExaminePlayer();
        }

        /// <summary>
        /// When the server heartbeat occurs, process auto-restart tasks.
        /// </summary>
        public void ProcessAutoRestart()
        {
            _serverTasks.ProcessAutoRestart();
        }

        /// <summary>
        /// When enmity changes, update debug enmity display.
        /// </summary>
        public void OnEnmityChanged()
        {
            _debugEnmityViewModel.OnEnmityChanged();
        }

        /// <summary>
        /// When a player targets something, handle creature manager targeting.
        /// </summary>
        public void RunTargetedLocationAction()
        {
            CreatureManagerViewModel.RunTargetedLocationAction();
        }

        /// <summary>
        /// When a player sends a chat message, audit the message.
        /// </summary>
        public void AuditChatMessages()
        {
            _auditingService.AuditChatMessages();
        }
    }
}
