using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ChangeDescriptionViewModel: GuiViewModelBase<ChangeDescriptionViewModel, GuiPayloadBase>
    {
        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
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
            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
        };
    }
}
