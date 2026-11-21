using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// Player station rules (plan section 25): anchor sites, founding, upkeep, the exterior and the exchange. A station is a
    /// player city whose land is a deck instance, so everything else follows the city rules.
    /// </summary>
    public static class StationRules
    {
        private static readonly Dictionary<string, StationAnchor> _anchorsById =
            StationTuning.Anchors.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, StationAnchor> _anchorsByTag =
            StationTuning.Anchors.ToDictionary(x => x.Tag, StringComparer.OrdinalIgnoreCase);

        /// <summary>The anchor with this id, or null.</summary>
        public static StationAnchor GetAnchor(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
                return null;

            return _anchorsById.TryGetValue(anchorId, out var anchor) ? anchor : null;
        }

        /// <summary>The anchor whose waypoint carries this tag, or null.</summary>
        public static StationAnchor GetAnchorByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            return _anchorsByTag.TryGetValue(tag, out var anchor) ? anchor : null;
        }

        public static IEnumerable<StationAnchor> GetAnchorsInArea(string areaResref)
        {
            return StationTuning.Anchors.Where(x => string.Equals(x.AreaResref, areaResref, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The nearest anchor in the area within founding range of the point, or null.</summary>
        public static StationAnchor FindAnchorInRange(string areaResref, float x, float y)
        {
            StationAnchor nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var anchor in GetAnchorsInArea(areaResref))
            {
                var distance = Distance(anchor.X, anchor.Y, x, y);
                if (distance > StationTuning.FoundingRange || distance >= nearestDistance)
                    continue;

                nearest = anchor;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>One station per site: a site is free when no station stands at it.</summary>
        public static bool IsSiteFree(string anchorId, IEnumerable<string> takenAnchorIds)
        {
            if (GetAnchor(anchorId) == null)
                return false;

            return !takenAnchorIds.Any(x => string.Equals(x, anchorId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Why the Station Core cannot found a station here, or <see cref="StationFoundingRefusal.None"/>.</summary>
        public static StationFoundingRefusal GetFoundingRefusal(StationFoundingRequest request)
        {
            if (!request.InSpace)
                return StationFoundingRefusal.NotInSpace;
            if (request.Anchor == null)
                return StationFoundingRefusal.NoAnchorInRange;
            if (!ShipAccessRules.AreaAdmits(ShipSize.Capital, request.AreaMaxSize))
                return StationFoundingRefusal.AreaSizeLimit;
            if (request.AnchorTaken)
                return StationFoundingRefusal.SiteTaken;
            if (request.IsCitizen)
                return StationFoundingRefusal.AlreadyCitizen;
            if (request.CityManagementLevel < StationTuning.MinimumCityManagement)
                return StationFoundingRefusal.MissingCityManagement;
            if (request.Credits < StationTuning.InitialPrice)
                return StationFoundingRefusal.InsufficientCredits;

            return StationFoundingRefusal.None;
        }

        public static string DescribeFoundingRefusal(StationFoundingRefusal refusal) => refusal switch
        {
            StationFoundingRefusal.NotInSpace => "A Station Core can only be deployed in space.",
            StationFoundingRefusal.NoAnchorInRange => $"There is no station anchor site within {(int)StationTuning.FoundingRange} meters.",
            StationFoundingRefusal.AreaSizeLimit => "Stations cannot be founded in a system that limits hull sizes.",
            StationFoundingRefusal.SiteTaken => "A station already stands at this anchor site.",
            StationFoundingRefusal.AlreadyCitizen => "You are already a citizen of another city. Revoke your citizenship first and try again.",
            StationFoundingRefusal.MissingCityManagement => "The City Management I perk is required to found a station.",
            StationFoundingRefusal.InsufficientCredits => $"Founding a station costs {StationTuning.InitialPrice} credits.",
            _ => string.Empty,
        };

        /// <summary>
        /// A station's weekly upkeep: the planet city's formula at the station layout's daily price, with its Jump Beacon
        /// level counted like the other building upgrades.
        /// </summary>
        public static int GetWeeklyUpkeep(int upkeepPerkLevel, int medicalCenterLevel, int starportLevel, int cantinaLevel, int jumpBeaconLevel = 1)
        {
            return CityUpkeepRules.GetWeeklyUpkeep(StationTuning.PricePerDay, upkeepPerkLevel, medicalCenterLevel, starportLevel, cantinaLevel, jumpBeaconLevel);
        }

        /// <summary>Where the station exterior stands: <see cref="StationTuning.ExteriorOffset"/> from the anchor toward the area's middle.</summary>
        public static (float X, float Y) GetExteriorPosition(StationAnchor anchor)
        {
            const float Middle = 160f;
            var dx = Middle - anchor.X;
            var dy = Middle - anchor.Y;
            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0f)
                return (anchor.X + StationTuning.ExteriorOffset, anchor.Y);

            return (anchor.X + dx / length * StationTuning.ExteriorOffset, anchor.Y + dy / length * StationTuning.ExteriorOffset);
        }

        /// <summary>The exterior's scale at a city level (1-5; out-of-range levels clamp).</summary>
        public static float GetExteriorScale(int cityLevel)
        {
            var scales = StationTuning.ExteriorScaleByLevel;
            var index = Math.Clamp(cityLevel, 1, scales.Length) - 1;
            return scales[index];
        }

        /// <summary>
        /// The NPC exchange a station's Trade Exchange trades through: its orbit's dock, or for Wild Space (which has no
        /// NPC exchange) the nearest open port. Invalid for an unknown anchor.
        /// </summary>
        public static TradeExchangeType GetExchange(string anchorId)
        {
            return GetAnchor(anchorId)?.Exchange ?? TradeExchangeType.Invalid;
        }

        private static float Distance(float x1, float y1, float x2, float y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
