using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class TradeExchangePayload : GuiPayloadBase
    {
        public TradeExchangeType Exchange { get; set; }

        /// <summary>
        /// The dock: an NPC starport's landing area resref, or a player city starport's property id. Ships docked here
        /// are the ones that trade.
        /// </summary>
        public string DockId { get; set; }

        public TradeExchangePayload(TradeExchangeType exchange, string dockId)
        {
            Exchange = exchange;
            DockId = dockId;
        }
    }
}
