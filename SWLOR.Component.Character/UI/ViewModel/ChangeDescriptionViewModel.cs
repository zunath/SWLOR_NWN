using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class ChangeDescriptionViewModel: GuiViewModelBase<ChangeDescriptionViewModel, IGuiPayload>
    {
        public ChangeDescriptionViewModel(IGuiService guiService) : base(guiService)
        {
        }
        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            var description = GetDescription(Player);
            if (string.IsNullOrWhiteSpace(description))
                description = string.Empty;

            Description = description;
            WatchOnClient(model => model.Description);
        }

        public Action OnClickResetToOriginal() => () =>
        {
            ShowModal("This will reset your description back to its original text. Are you sure you want to do this?", () =>
            {
                Description = GetDescription(Player, true);
            });
        };

        public Action OnClickSave() => () =>
        {
            if (string.IsNullOrWhiteSpace(Description))
            {
                FloatingTextStringOnCreature("Please enter a description.", Player, false);
                return;
            }

            SetDescription(Player, Description);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };

        public Action OnClickCancel() => () =>
        {
            _guiService.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };
    }
}
