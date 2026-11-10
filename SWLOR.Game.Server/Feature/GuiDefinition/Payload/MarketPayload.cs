using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Economy;

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
