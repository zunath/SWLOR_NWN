namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What <see cref="ShipAccessRules.GetStarportRefusal"/> checks when a ship docks at a starport.</summary>
    public sealed class StarportDockRequest
    {
        public ShipSize ShipSize { get; init; }

        /// <summary>The dock is a player-city starport; otherwise it is an NPC dock.</summary>
        public bool IsPlayerStarport { get; init; }

        /// <summary>The player starport stands on a player station's deck (plan section 25); the station rule is the city's.</summary>
        public bool IsStationStarport { get; init; }

        /// <summary>The NPC dock's area carries the <c>CAPITAL_DOCK</c> flag (one of the major ports).</summary>
        public bool CapitalDock { get; init; }

        /// <summary>The effective starport upgrade level of the player starport's city.</summary>
        public int PlayerStarportLevel { get; init; }

        /// <summary>The dock area's <c>MAX_SHIP_SIZE</c>; <see cref="ShipSize.Invalid"/> when the area has none.</summary>
        public ShipSize AreaMaxSize { get; init; }
    }
}
