using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Communication.EventHandlers
{
    internal class CommunicationServiceEventHandlers
    {
        private readonly IDialogService _dialogService;
        private readonly ISnippetService _snippetService;

        public CommunicationServiceEventHandlers(
            IDialogService dialogService,
            ISnippetService snippetService,
            IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _snippetService = snippetService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => InitializeDialogs());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => RegisterConversations());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheSnippetData());
            eventAggregator.Subscribe<OnDialogStart>(e => Start());
            eventAggregator.Subscribe<OnDialogAction0>(e => NodeAction0());
            eventAggregator.Subscribe<OnDialogAction1>(e => NodeAction1());
            eventAggregator.Subscribe<OnDialogAction2>(e => NodeAction2());
            eventAggregator.Subscribe<OnDialogAction3>(e => NodeAction3());
            eventAggregator.Subscribe<OnDialogAction4>(e => NodeAction4());
            eventAggregator.Subscribe<OnDialogAction5>(e => NodeAction5());
            eventAggregator.Subscribe<OnDialogAction6>(e => NodeAction6());
            eventAggregator.Subscribe<OnDialogAction7>(e => NodeAction7());
            eventAggregator.Subscribe<OnDialogAction8>(e => NodeAction8());
            eventAggregator.Subscribe<OnDialogAction9>(e => NodeAction9());
            eventAggregator.Subscribe<OnDialogAction10>(e => NodeAction10());
            eventAggregator.Subscribe<OnDialogAction11>(e => NodeAction11());
            eventAggregator.Subscribe<OnDialogActionNext>(e => NextAction());
            eventAggregator.Subscribe<OnDialogActionPrevious>(e => PreviousAction());
            eventAggregator.Subscribe<OnDialogActionBack>(e => BackAction());
            eventAggregator.Subscribe<OnDialogEnd>(e => End());
            eventAggregator.Subscribe<OnDialogStartConversation>(e => StartConversationEvent());
            eventAggregator.SubscribeConditional<OnDialogAppears0>(e => NodeAppears0());
            eventAggregator.SubscribeConditional<OnDialogAppears1>(e => NodeAppears1());
            eventAggregator.SubscribeConditional<OnDialogAppears2>(e => NodeAppears2());
            eventAggregator.SubscribeConditional<OnDialogAppears3>(e => NodeAppears3());
            eventAggregator.SubscribeConditional<OnDialogAppears4>(e => NodeAppears4());
            eventAggregator.SubscribeConditional<OnDialogAppears5>(e => NodeAppears5());
            eventAggregator.SubscribeConditional<OnDialogAppears6>(e => NodeAppears6());
            eventAggregator.SubscribeConditional<OnDialogAppears7>(e => NodeAppears7());
            eventAggregator.SubscribeConditional<OnDialogAppears8>(e => NodeAppears8());
            eventAggregator.SubscribeConditional<OnDialogAppears9>(e => NodeAppears9());
            eventAggregator.SubscribeConditional<OnDialogAppears10>(e => NodeAppears10());
            eventAggregator.SubscribeConditional<OnDialogAppears11>(e => NodeAppears11());
            eventAggregator.SubscribeConditional<OnDialogAppearsHeader>(e => HeaderAppearsWhen());
            eventAggregator.SubscribeConditional<OnDialogAppearsNext>(e => NextAppearsWhen());
            eventAggregator.SubscribeConditional<OnDialogAppearsPrevious>(e => PreviousAppearsWhen());
            eventAggregator.SubscribeConditional<OnDialogAppearsB>(e => BackAppearsWhen());
            eventAggregator.SubscribeConditional<OnDialogAppear>(e => ConversationAppearsWhen());
            eventAggregator.Subscribe<OnDialogAction>(e => ConversationAction());
        }

        /// <summary>
        /// When the module loads, initialize dialogs.
        /// </summary>
        public void InitializeDialogs()
        {
            _dialogService.InitializeDialogs();
        }

        /// <summary>
        /// When the module is loaded, the assembly will be searched for conversations.
        /// These will be added to the cache for use at a later time.
        /// </summary>
        public void RegisterConversations()
        {
            _dialogService.RegisterConversations();
        }

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        public void CacheSnippetData()
        {
            _snippetService.CacheData();
        }

        /// <summary>
        /// Handles when a dialog is started.
        /// </summary>
        public void Start()
        {
            _dialogService.Start();
        }
        public void NodeAction0()
        {
            _dialogService.NodeAction0();
        }
        public void NodeAction1()
        {
            _dialogService.NodeAction1();
        }
        public void NodeAction2()
        {
            _dialogService.NodeAction2();
        }
        public void NodeAction3()
        {
            _dialogService.NodeAction3();
        }
        public void NodeAction4()
        {
            _dialogService.NodeAction4();
        }
        public void NodeAction5()
        {
            _dialogService.NodeAction5();
        }
        public void NodeAction6()
        {
            _dialogService.NodeAction6();
        }
        public void NodeAction7()
        {
            _dialogService.NodeAction7();
        }
        public void NodeAction8()
        {
            _dialogService.NodeAction8();
        }
        public void NodeAction9()
        {
            _dialogService.NodeAction9();
        }
        public void NodeAction10()
        {
            _dialogService.NodeAction10();
        }
        public void NodeAction11()
        {
            _dialogService.NodeAction11();
        }
        public void NextAction()
        {
            _dialogService.NextAction();
        }
        public void PreviousAction()
        {
            _dialogService.PreviousAction();
        }
        public void BackAction()
        {
            _dialogService.BackAction();
        }

        /// <summary>
        /// Fires when the "End Dialog" node is clicked.
        /// </summary>
        public void End()
        {
            _dialogService.End();
        }

        /// <summary>
        /// When an object executes this script, the custom dialog specified on their local variables
        /// will be started.
        /// </summary>
        public void StartConversationEvent()
        {
            _dialogService.StartConversationEvent();
        }

        // Dialog conditional methods - these return boolean values to determine visibility
        public bool NodeAppears0()
        {
            return _dialogService.NodeAppears0();
        }
        public bool NodeAppears1()
        {
            return _dialogService.NodeAppears1();
        }
        public bool NodeAppears2()
        {
            return _dialogService.NodeAppears2();
        }
        public bool NodeAppears3()
        {
            return _dialogService.NodeAppears3();
        }
        public bool NodeAppears4()
        {
            return _dialogService.NodeAppears4();
        }
        public bool NodeAppears5()
        {
            return _dialogService.NodeAppears5();
        }
        public bool NodeAppears6()
        {
            return _dialogService.NodeAppears6();
        }
        public bool NodeAppears7()
        {
            return _dialogService.NodeAppears7();
        }
        public bool NodeAppears8()
        {
            return _dialogService.NodeAppears8();
        }
        public bool NodeAppears9()
        {
            return _dialogService.NodeAppears9();
        }
        public bool NodeAppears10()
        {
            return _dialogService.NodeAppears10();
        }
        public bool NodeAppears11()
        {
            return _dialogService.NodeAppears11();
        }
        public bool HeaderAppearsWhen()
        {
            return _dialogService.HeaderAppearsWhen();
        }
        public bool NextAppearsWhen()
        {
            return _dialogService.NextAppearsWhen();
        }
        public bool PreviousAppearsWhen()
        {
            return _dialogService.PreviousAppearsWhen();
        }
        public bool BackAppearsWhen()
        {
            return _dialogService.BackAppearsWhen();
        }



        public bool ConversationAppearsWhen()
        {
            return _snippetService.ConversationAppearsWhen();
        }


        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>

        public void ConversationAction()
        {
            _snippetService.ConversationAction();
        }
    }
}
