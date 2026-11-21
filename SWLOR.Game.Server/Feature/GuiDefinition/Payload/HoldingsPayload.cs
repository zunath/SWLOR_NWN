using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class HoldingsPayload : GuiPayloadBase
    {
        /// <summary>The dock exchange the window was opened at: the holdings it sells.</summary>
        public TradeExchangeType Exchange { get; set; }

        /// <summary>The NPC starport's landing area resref. Ships docked here collect and deliver.</summary>
        public string DockId { get; set; }

        /// <summary>The holding to show first, or empty.</summary>
        public string HoldingId { get; set; }

        public HoldingsPayload(TradeExchangeType exchange, string dockId, string holdingId = "")
        {
            Exchange = exchange;
            DockId = dockId;
            HoldingId = holdingId;
        }
    }
}
