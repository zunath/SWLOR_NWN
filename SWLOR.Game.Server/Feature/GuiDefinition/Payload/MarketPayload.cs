using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class MarketPayload: GuiPayloadBase
    {
        public MarketRegionType RegionType { get; set; }

        public MarketPayload(MarketRegionType regionType)
        {
            RegionType = regionType;
        }
    }
}
