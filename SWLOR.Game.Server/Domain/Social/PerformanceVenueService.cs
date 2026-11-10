using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The venue rating of a performance: the logic that used to be static in <c>PerformanceVenue</c>,
    /// moved here unchanged. Open world is 0, an NPC cantina sets the PERFORMANCE_VENUE_RATING area variable, and a player
    /// cantina takes its city's Cantina level. Each point adds 1% skill XP, 10% banking and 12 minutes of Inspired cap.
    /// The area ratings and the Dance Floor resref this instance looks up are its own state.
    /// </summary>
    public class PerformanceVenueService : IPerformanceVenueService
    {
        public const string AreaRatingVariable = "PERFORMANCE_VENUE_RATING";
        public const float DanceFloorRadiusMeters = 3f;
        public const int DanceFloorBonus = 1;
        private const int MaxCachedAreas = 64;

        private readonly IDBService _db;
        private readonly IPropertyService _property;

        // A city's Cantina level changes rarely, so an area's rating is looked up at most this often.
        private readonly TimeSpan _areaRatingLifetime = TimeSpan.FromMinutes(5);
        private readonly Dictionary<uint, (int Rating, DateTime ExpiresUtc)> _areaRatings = new();
        private string _danceFloorResref;

        public PerformanceVenueService(IDBService db, IPropertyService property)
        {
            _db = db;
            _property = property;
        }

        public int Combine(int areaRating, bool danceFloorNearby)
        {
            return PerformanceMath.ClampVenue(Math.Max(0, areaRating) + (danceFloorNearby ? DanceFloorBonus : 0));
        }

        public int GetRating(uint performer)
        {
            return Combine(GetAreaRating(GetArea(performer)), HasDanceFloorNearby(performer));
        }

        public PerformanceVenueBreakdown GetBreakdown(uint performer)
        {
            var area = GetArea(performer);
            var areaRating = GetAreaRating(area);
            var danceFloor = HasDanceFloorNearby(performer);
            return new PerformanceVenueBreakdown(
                Combine(areaRating, danceFloor), areaRating, DescribeArea(area, areaRating), danceFloor);
        }

        private string DescribeArea(uint area, int rating)
        {
            if (rating <= 0)
                return "Open area, no venue rating";

            return GetLocalInt(area, AreaRatingVariable) > 0
                ? "Cantina"
                : $"Player cantina, city Cantina Lvl {rating}";
        }

        public int GetAreaRating(uint area)
        {
            if (!GetIsObjectValid(area))
                return 0;

            var now = UtcNow();
            if (_areaRatings.TryGetValue(area, out var cached) && cached.ExpiresUtc > now)
                return cached.Rating;

            var rating = PerformanceMath.ClampVenue(ReadAreaRating(area));
            ForgetExpiredAreaRatings(now);
            _areaRatings[area] = (rating, now + _areaRatingLifetime);
            return rating;
        }

        // Instance areas such as cantina interiors come and go, so lapsed entries are dropped as the cache grows.
        private void ForgetExpiredAreaRatings(DateTime now)
        {
            if (_areaRatings.Count < MaxCachedAreas)
                return;

            var expired = new List<uint>();
            foreach (var (area, cached) in _areaRatings)
            {
                if (cached.ExpiresUtc <= now)
                    expired.Add(area);
            }

            foreach (var area in expired)
                _areaRatings.Remove(area);
        }

        public bool HasDanceFloorNearby(uint performer)
        {
            var resref = GetDanceFloorResref();
            foreach (var placeable in GetPlaceablesNear(performer))
            {
                if (GetResRef(placeable) == resref)
                    return true;
            }

            return false;
        }

        private int ReadAreaRating(uint area)
        {
            var authored = GetLocalInt(area, AreaRatingVariable);
            if (authored > 0)
                return authored;

            return GetPlayerCantinaRating(area);
        }

        // Interior area -> its property -> the cantina building -> the city whose Cantina level is the rating.
        private int GetPlayerCantinaRating(uint area)
        {
            var propertyId = _property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
                return 0;

            var interior = _db.Get<WorldProperty>(propertyId);
            if (interior == null ||
                interior.PropertyType != PropertyType.Cantina ||
                string.IsNullOrWhiteSpace(interior.ParentPropertyId))
                return 0;

            var building = _db.Get<WorldProperty>(interior.ParentPropertyId);
            if (building == null || string.IsNullOrWhiteSpace(building.ParentPropertyId))
                return 0;

            return _property.GetEffectiveUpgradeLevel(building.ParentPropertyId, PropertyUpgradeType.CantinaLevel);
        }

        private string GetDanceFloorResref()
        {
            return _danceFloorResref ??= _property.GetStructureByType(StructureType.DanceFloor).Resref;
        }

        /// <summary>The time now. A test sets it to expire the cached ratings.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        /// <summary>The placeables within the Dance Floor radius of the performer; the shape search needs a real engine location.</summary>
        protected virtual IEnumerable<uint> GetPlaceablesNear(uint performer) =>
            ObjectSnapshot.InShape(Shape.Sphere, DanceFloorRadiusMeters, GetLocation(performer), false, ObjectType.Placeable);
    }
}
