using SWLOR.Component.Market.Enums;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Market.UI.Payload
{
    public class MarketPayload: IGuiPayload
    {
        public MarketRegionType RegionType { get; set; }

        public MarketPayload(MarketRegionType regionType)
        {
            RegionType = regionType;
        }
    }
}
