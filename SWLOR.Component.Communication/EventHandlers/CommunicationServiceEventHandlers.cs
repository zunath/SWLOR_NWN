using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Dialog;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Communication.EventHandlers
{
    internal class CommunicationServiceEventHandlers
    {
        private readonly IDialogService _dialogService;
        private readonly ISnippetService _snippetService;

        public CommunicationServiceEventHandlers(
            IDialogService dialogService,
            ISnippetService snippetService)
        {
            _dialogService = dialogService;
            _snippetService = snippetService;
        }

        /// <summary>
        /// When the module loads, initialize dialogs.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void InitializeDialogs()
        {
            _dialogService.InitializeDialogs();
        }

        /// <summary>
        /// When the module is loaded, the assembly will be searched for conversations.
        /// These will be added to the cache for use at a later time.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void RegisterConversations()
        {
            _dialogService.RegisterConversations();
        }

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheSnippetData()
        {
            _snippetService.CacheData();
        }

        /// <summary>
        /// Handles when a dialog is started.
        /// </summary>
        [ScriptHandler<OnDialogStart>]
        public void Start()
        {
            _dialogService.Start();
        }

        [ScriptHandler<OnDialogAction0>]
        public void NodeAction0()
        {
            _dialogService.NodeAction0();
        }

        [ScriptHandler<OnDialogAction1>]
        public void NodeAction1()
        {
            _dialogService.NodeAction1();
        }

        [ScriptHandler<OnDialogAction2>]
        public void NodeAction2()
        {
            _dialogService.NodeAction2();
        }

        [ScriptHandler<OnDialogAction3>]
        public void NodeAction3()
        {
            _dialogService.NodeAction3();
        }

        [ScriptHandler<OnDialogAction4>]
        public void NodeAction4()
        {
            _dialogService.NodeAction4();
        }

        [ScriptHandler<OnDialogAction5>]
        public void NodeAction5()
        {
            _dialogService.NodeAction5();
        }

        [ScriptHandler<OnDialogAction6>]
        public void NodeAction6()
        {
            _dialogService.NodeAction6();
        }

        [ScriptHandler<OnDialogAction7>]
        public void NodeAction7()
        {
            _dialogService.NodeAction7();
        }

        [ScriptHandler<OnDialogAction8>]
        public void NodeAction8()
        {
            _dialogService.NodeAction8();
        }

        [ScriptHandler<OnDialogAction9>]
        public void NodeAction9()
        {
            _dialogService.NodeAction9();
        }

        [ScriptHandler<OnDialogAction10>]
        public void NodeAction10()
        {
            _dialogService.NodeAction10();
        }

        [ScriptHandler<OnDialogAction11>]
        public void NodeAction11()
        {
            _dialogService.NodeAction11();
        }

        [ScriptHandler<OnDialogActionNext>]
        public void NextAction()
        {
            _dialogService.NextAction();
        }

        [ScriptHandler<OnDialogActionPrevious>]
        public void PreviousAction()
        {
            _dialogService.PreviousAction();
        }

        [ScriptHandler<OnDialogActionBack>]
        public void BackAction()
        {
            _dialogService.BackAction();
        }

        /// <summary>
        /// Fires when the "End Dialog" node is clicked.
        /// </summary>
        [ScriptHandler<OnDialogEnd>]
        public void End()
        {
            _dialogService.End();
        }

        /// <summary>
        /// When an object executes this script, the custom dialog specified on their local variables
        /// will be started.
        /// </summary>
        [ScriptHandler<OnDialogStartConversation>]
        public void StartConversationEvent()
        {
            _dialogService.StartConversationEvent();
        }

        // Dialog conditional methods - these return boolean values to determine visibility
        [ScriptHandler<OnDialogAppears0>]
        public bool NodeAppears0()
        {
            return _dialogService.NodeAppears0();
        }

        [ScriptHandler<OnDialogAppears1>]
        public bool NodeAppears1()
        {
            return _dialogService.NodeAppears1();
        }

        [ScriptHandler<OnDialogAppears2>]
        public bool NodeAppears2()
        {
            return _dialogService.NodeAppears2();
        }

        [ScriptHandler<OnDialogAppears3>]
        public bool NodeAppears3()
        {
            return _dialogService.NodeAppears3();
        }

        [ScriptHandler<OnDialogAppears4>]
        public bool NodeAppears4()
        {
            return _dialogService.NodeAppears4();
        }

        [ScriptHandler<OnDialogAppears5>]
        public bool NodeAppears5()
        {
            return _dialogService.NodeAppears5();
        }

        [ScriptHandler<OnDialogAppears6>]
        public bool NodeAppears6()
        {
            return _dialogService.NodeAppears6();
        }

        [ScriptHandler<OnDialogAppears7>]
        public bool NodeAppears7()
        {
            return _dialogService.NodeAppears7();
        }

        [ScriptHandler<OnDialogAppears8>]
        public bool NodeAppears8()
        {
            return _dialogService.NodeAppears8();
        }

        [ScriptHandler<OnDialogAppears9>]
        public bool NodeAppears9()
        {
            return _dialogService.NodeAppears9();
        }

        [ScriptHandler<OnDialogAppears10>]
        public bool NodeAppears10()
        {
            return _dialogService.NodeAppears10();
        }

        [ScriptHandler<OnDialogAppears11>]
        public bool NodeAppears11()
        {
            return _dialogService.NodeAppears11();
        }

        [ScriptHandler<OnDialogAppearsHeader>]
        public bool HeaderAppearsWhen()
        {
            return _dialogService.HeaderAppearsWhen();
        }

        [ScriptHandler<OnDialogAppearsNext>]
        public bool NextAppearsWhen()
        {
            return _dialogService.NextAppearsWhen();
        }

        [ScriptHandler<OnDialogAppearsPrevious>]
        public bool PreviousAppearsWhen()
        {
            return _dialogService.PreviousAppearsWhen();
        }

        [ScriptHandler<OnDialogAppearsB>]
        public bool BackAppearsWhen()
        {
            return _dialogService.BackAppearsWhen();
        }

        [ScriptHandler<OnDialogAppear>]
        [ScriptHandler<OnDialogAppears>]
        [ScriptHandler<OnDialogCondition>]
        [ScriptHandler<OnDialogConditions>]
        public bool ConversationAppearsWhen()
        {
            return _snippetService.ConversationAppearsWhen();
        }


        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [ScriptHandler<OnDialogAction>]
        [ScriptHandler<OnDialogActions>]
        public void ConversationAction()
        {
            _snippetService.ConversationAction();
        }
    }
}
