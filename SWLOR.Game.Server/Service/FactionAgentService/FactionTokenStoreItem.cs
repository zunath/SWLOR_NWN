using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>An item a faction token store sells for that faction's tokens.</summary>
    public class FactionTokenStoreItem
    {
        public FactionTokenStoreCategory Category { get; set; }
        public string Resref { get; set; }

        /// <summary>The name shown in the store; empty to use the item's own name.</summary>
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
        public int Tokens { get; set; }
        public TradeStandingTier MinimumTier { get; set; }

        /// <summary>A manufacturer component's maker trait, stamped on the item when bought.</summary>
        public ShipComponentTraitType Trait { get; set; }
    }
}
