using System;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
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
