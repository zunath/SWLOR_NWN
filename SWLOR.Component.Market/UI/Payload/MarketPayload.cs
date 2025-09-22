using SWLOR.Component.Market.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Market.UI.Payload
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
