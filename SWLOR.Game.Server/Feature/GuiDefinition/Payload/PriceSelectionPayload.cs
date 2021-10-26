using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PriceSelectionPayload: GuiPayloadBase
    {
        public GuiWindowType WindowType { get; set; }
        public string RecordId { get; set; }
        public int CurrentPrice { get; set; }
        public string ItemName { get; set; }

        public PriceSelectionPayload(
            GuiWindowType windowType,
            string recordId,
            int currentPrice,
            string itemName)
        {
            WindowType = windowType;
            RecordId = recordId;
            CurrentPrice = currentPrice;
            ItemName = itemName;
        }
    }
}
