namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The dock a Trade Exchange window trades at: an NPC starport (identified by its landing area resref) or a player
    /// city's starport (identified by the starport's property id, which is also the id a docked ship's dock position
    /// keeps). A city dock trades through its planet's NPC exchange and charges the city's tariff.
    /// </summary>
    public class TradeDock
    {
        /// <summary>The landing area resref of an NPC dock, or the starport property id of a city dock.</summary>
        public string DockId { get; set; } = string.Empty;

        /// <summary>The NPC exchange this dock trades through.</summary>
        public TradeExchangeType Exchange { get; set; }

        public bool IsCity { get; set; }

        /// <summary>The city property id, or empty at an NPC dock.</summary>
        public string CityId { get; set; } = string.Empty;

        public string CityName { get; set; } = string.Empty;

        /// <summary>The tariff percent (0 at an NPC dock).</summary>
        public int TariffPercent { get; set; }

        /// <summary>The city's effective starport upgrade level (0 at an NPC dock).</summary>
        public int StarportLevel { get; set; }

        /// <summary>Whether this dock has a Trade Exchange: an NPC dock always, a city one from the starport level that opens it.</summary>
        public bool HasExchange => Exchange != TradeExchangeType.Invalid && (!IsCity || TradeTariffRules.IsExchangeAvailable(StarportLevel));

        public static TradeDock Npc(string dockId, TradeExchangeType exchange)
        {
            return new TradeDock { DockId = dockId ?? string.Empty, Exchange = exchange };
        }
    }
}
