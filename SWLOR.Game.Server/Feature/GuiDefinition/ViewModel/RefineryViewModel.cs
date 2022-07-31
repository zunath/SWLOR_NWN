using System;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RefineryViewModel: GuiViewModelBase<RefineryViewModel, GuiPayloadBase>
    {
        private const int BaseItemsRefinedPerCore = 3;
        private const string PowerCoreIconResref = "iit_midmisc_008";

        public GuiBindingList<string> InputItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OutputItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string RequiredPowerCores
        {
            get => Get<string>();
            set => Set(value);
        }


        protected override void Initialize(GuiPayloadBase initialPayload)
        {

        }

        public Action OnClickAddItem() => () =>
        {

        };

        public Action OnClickRemoveItems() => () =>
        {

        };

        public Action OnClickRefine() => () =>
        {

        };
    }
}
