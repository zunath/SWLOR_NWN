using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ModalViewModel: GuiViewModelBase<ModalViewModel, GuiPayloadBase>
    {
        public string PromptText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ConfirmButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CancelButtonText
        {
            get => Get<string>();
            set => Set(value);
        }


        public void LoadModalInfo(GuiRectangle parentGeometry, string prompt, Action confirmAction, Action cancelAction, string confirmText, string cancelText)
        {
            _parentGeometry = parentGeometry;
            PromptText = prompt;
            ConfirmButtonText = confirmText;
            CancelButtonText = cancelText;
            _callerConfirmAction = confirmAction;
            _callerCancelAction = cancelAction;
        }

        private GuiRectangle _parentGeometry;
        private Action _callerConfirmAction;
        private Action _callerCancelAction;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = _parentGeometry;
        }

        public Action OnWindowClose() => () =>
        {
            // Reset to default values.
            PromptText = "Are you sure?";

            ConfirmButtonText = "Yes";
            CancelButtonText = "No";

            _callerConfirmAction = null;
            _callerCancelAction = null;
        };

        public Action OnConfirmClick() => () =>
        {
            if (_callerConfirmAction != null)
                _callerConfirmAction();

            Gui.CloseModal(Player, WindowType);
        };

        public Action OnCancelClick() => () =>
        {
            if (_callerCancelAction != null)
                _callerCancelAction();

            Gui.CloseModal(Player, WindowType);
        };
    }
}
