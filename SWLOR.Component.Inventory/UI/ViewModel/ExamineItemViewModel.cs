using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Inventory.UI.ViewModel
{
    public class ExamineItemViewModel: GuiViewModelBase<ExamineItemViewModel, ExamineItemPayload>
    {
        public ExamineItemViewModel(IGuiService guiService) : base(guiService)
        {
        }

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ItemProperties
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(ExamineItemPayload initialPayload)
        {
            WindowTitle = initialPayload.ItemName;
            Description = initialPayload.Description;
            ItemProperties = initialPayload.ItemProperties;
        }
    }
}
