using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    public class PriceSelectionPayload: GuiPayloadBase
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
