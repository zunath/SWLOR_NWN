using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Events.Attributes;
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
    }
}
