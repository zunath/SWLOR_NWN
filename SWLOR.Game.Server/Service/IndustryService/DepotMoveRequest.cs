using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>The facts the Planetary Depot checks before it moves a holding to the dock the owner stands at.</summary>
    public sealed class DepotMoveRequest
    {
        public bool OwnerOwnsHolding { get; init; } = true;

        /// <summary>The exchange of the planet the holding stands on.</summary>
        public TradeExchangeType HoldingExchange { get; init; }

        /// <summary>The holdings exchange of the dock the owner stands at.</summary>
        public TradeExchangeType DockExchange { get; init; }

        public string HoldingDockId { get; init; } = string.Empty;
        public string DockId { get; init; } = string.Empty;

        /// <summary>Whether the holding now waits at a player city's or station's Starport.</summary>
        public bool HoldingAtCityStarport { get; init; }

        /// <summary>Whether the dock is a player city's or station's Starport.</summary>
        public bool DockIsCityStarport { get; init; }

        /// <summary>The city Starport's effective upgrade level. Ignored at an NPC dock.</summary>
        public int StarportLevel { get; init; }

        /// <summary>Whether the owner is a citizen of the city whose Starport it is. Ignored at an NPC dock.</summary>
        public bool OwnerIsCitizen { get; init; }

        /// <summary>When the holding last moved by the Depot; the default time if it never has.</summary>
        public DateTime LastMoveUtc { get; init; }

        public DateTime NowUtc { get; init; }
        public bool PaymentPending { get; init; }
    }
}
