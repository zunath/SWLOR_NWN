using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.PlayerMarketService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
