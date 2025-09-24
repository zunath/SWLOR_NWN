using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.UI.Payloads
{
    public class ExamineItemPayload: IGuiPayload
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
