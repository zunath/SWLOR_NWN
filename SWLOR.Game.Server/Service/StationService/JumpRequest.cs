using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>What <see cref="StationBuildingRules.GetJumpRefusal"/> checks before a Jump Beacon jumps a docked ship.</summary>
    public sealed class JumpRequest
    {
        /// <summary>The ship is docked at one of the origin station's Starports and nobody is flying it.</summary>
        public bool ShipDocked { get; init; }

        /// <summary>The player owns the ship or may pilot it.</summary>
        public bool CanPilot { get; init; }

        public bool OriginInWildSpace { get; init; }
        public bool DestinationInWildSpace { get; init; }
        public bool SameStation { get; init; }

        /// <summary>The origin beacon's access setting admits the player.</summary>
        public bool OriginPermits { get; init; }

        /// <summary>The destination beacon's access setting admits the player.</summary>
        public bool DestinationPermits { get; init; }

        public int OriginBeaconLevel { get; init; }
        public int DestinationBeaconLevel { get; init; }
        public ShipSize ShipSize { get; init; }

        /// <summary>The ship's hull deed tag, for hulls that burn less fuel.</summary>
        public string HullTag { get; init; } = string.Empty;

        /// <summary>An incursion's blockade holds at the destination station's dock (W11o).</summary>
        public bool DestinationBlockaded { get; init; }

        /// <summary>Station Fuel units in the ship's cargo bay.</summary>
        public int FuelAboard { get; init; }

        /// <summary>
        /// The cargo aboard as pirates value it (commodities at base price, sealed freight by volume), with the cargo of
        /// the ships docked aboard (<see cref="StationBuildingRules.GetJumpCargoValue"/>).
        /// </summary>
        public int CargoValue { get; init; }

        /// <summary>Starlane hops between the two stations' systems (their anchors' exchanges).</summary>
        public int Hops { get; init; }
    }
}
