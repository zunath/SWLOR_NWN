using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A good a Rift Raider wreck can spill as stolen cargo, with its weight and unit range.</summary>
    public class StolenCargoEntry
    {
        public CommodityType Commodity { get; init; }
        public int Weight { get; init; }
        public int MinimumUnits { get; init; }
        public int MaximumUnits { get; init; }
    }
}
