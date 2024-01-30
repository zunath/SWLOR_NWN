using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TargetDescriptionViewModel : GuiViewModelBase<TargetDescriptionViewModel, TargetDescriptionPayload>
    {
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
            Gui.TogglePlayerWindow(Player, GuiWindowType.TargetDescription);
        };
    }
}
