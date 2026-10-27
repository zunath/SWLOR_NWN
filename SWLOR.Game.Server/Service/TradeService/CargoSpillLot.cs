using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The commodity units one cargo pod holds after a ship is destroyed.</summary>
    public class CargoSpillLot
    {
        public Dictionary<CommodityType, int> Units { get; set; } = new();
    }
}
