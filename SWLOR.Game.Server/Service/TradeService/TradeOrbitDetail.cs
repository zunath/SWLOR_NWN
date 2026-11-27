using System;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A space system for trade risk (plan section 13): its tier (pirate raiders and convoys) and the customs patrols
    /// that fly it. Rows come from the generated TradeCatalog.
    /// </summary>
    public class TradeOrbitDetail
    {
        public string AreaResref { get; set; }
        public string Name { get; set; }
        public int Tier { get; set; }

        /// <summary>The lawful authorities whose patrols scan ships here. Empty in an open system.</summary>
        public TradeAuthority[] CustomsAuthorities { get; set; } = Array.Empty<TradeAuthority>();
    }
}
