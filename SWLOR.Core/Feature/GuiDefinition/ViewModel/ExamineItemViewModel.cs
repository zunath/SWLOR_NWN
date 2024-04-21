using SWLOR.Core.Feature.GuiDefinition.Payload;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.ViewModel
{
    public class ExamineItemViewModel: GuiViewModelBase<ExamineItemViewModel, ExamineItemPayload>
    {
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
