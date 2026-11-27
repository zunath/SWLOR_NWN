using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>What <see cref="StationRules.GetFoundingRefusal"/> checks when a Station Core is used.</summary>
    public sealed class StationFoundingRequest
    {
        /// <summary>The player is in a space area.</summary>
        public bool InSpace { get; init; }

        /// <summary>The free or taken anchor within founding range, or null when there is none.</summary>
        public StationAnchor Anchor { get; init; }

        /// <summary>The space area's <c>MAX_SHIP_SIZE</c>; Invalid when it has none.</summary>
        public ShipSize AreaMaxSize { get; init; }

        /// <summary>A station already stands at the anchor.</summary>
        public bool AnchorTaken { get; init; }

        /// <summary>The player is already a citizen of a city or station.</summary>
        public bool IsCitizen { get; init; }

        public int CityManagementLevel { get; init; }
        public int Credits { get; init; }
    }
}
