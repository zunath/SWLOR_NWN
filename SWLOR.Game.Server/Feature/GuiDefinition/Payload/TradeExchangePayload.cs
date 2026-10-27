using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class TradeExchangePayload : GuiPayloadBase
    {
        public TradeExchangeType Exchange { get; set; }

        /// <summary>The dock's landing area: ships docked here are the ones that trade.</summary>
        public string LandingAreaResref { get; set; }

        public TradeExchangePayload(TradeExchangeType exchange, string landingAreaResref)
        {
            Exchange = exchange;
            LandingAreaResref = landingAreaResref;
        }
    }
}
