using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
{
    public class ExamineItemPayload: GuiPayloadBase
    {
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string ItemProperties { get; set; }

        public ExamineItemPayload(string itemName, string description, string itemProperties)
        {
            ItemName = itemName;
            Description = description;
            ItemProperties = itemProperties;
        }
    }
}
