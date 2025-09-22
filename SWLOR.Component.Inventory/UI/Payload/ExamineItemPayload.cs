using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Inventory.UI.Payload
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
