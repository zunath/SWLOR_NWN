using SWLOR.Component.Communication.UI.Payload;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.UI.ViewModel
{
    public class TargetDescriptionViewModel : GuiViewModelBase<TargetDescriptionViewModel, TargetDescriptionPayload>
    {
        public TargetDescriptionViewModel(IGuiService guiService) : base(guiService)
        {
        }

        private uint _target;

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(TargetDescriptionPayload initialPayload)
        {
            _target = initialPayload.Target;

            var description = GetDescription(_target);
            if (string.IsNullOrWhiteSpace(description))
                description = string.Empty;

            Description = description;
            WatchOnClient(model => model.Description);
        }

        public Action OnClickSave() => () =>
        {
            SetDescription(_target, Description);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.TargetDescription);
        };
    }
}
