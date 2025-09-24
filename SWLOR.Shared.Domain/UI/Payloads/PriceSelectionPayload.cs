using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Domain.UI.Payloads
{
    public class PriceSelectionPayload: IGuiPayload
    {
        public GuiWindowType WindowType { get; set; }
        public string RecordId { get; set; }
        public int CurrentPrice { get; set; }
        public string ItemName { get; set; }
        public string PromptText { get; set; }

        public PriceSelectionPayload(
            GuiWindowType windowType,
            string recordId,
            int currentPrice,
            string itemName,
            string promptText)
        {
            WindowType = windowType;
            RecordId = recordId;
            CurrentPrice = currentPrice;
            ItemName = itemName;
            PromptText = promptText;
        }
    }
}
