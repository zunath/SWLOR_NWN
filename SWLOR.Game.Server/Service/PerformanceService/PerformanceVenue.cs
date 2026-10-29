using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The venue rating of a performance, 0 to 5: the area's rating plus one for a Dance Floor within 3 meters.
    /// Open world is 0, an NPC cantina sets the PERFORMANCE_VENUE_RATING area variable, and a player cantina takes its
    /// city's Cantina level. Each point adds 1% skill XP, 10% banking and 12 minutes of Inspired cap.
    /// </summary>
    public static class PerformanceVenue
    {
        public const string AreaRatingVariable = "PERFORMANCE_VENUE_RATING";
        public const float DanceFloorRadiusMeters = 3f;
        public const int DanceFloorBonus = 1;
        private const int MaxCachedAreas = 64;

        // A city's Cantina level changes rarely, so an area's rating is looked up at most this often.
        private static readonly TimeSpan _areaRatingLifetime = TimeSpan.FromMinutes(5);
        private static readonly Dictionary<uint, (int Rating, DateTime ExpiresUtc)> _areaRatings = new();
        private static string _danceFloorResref;

        /// <summary>Pure: the area's rating plus the Dance Floor point, held to 0 through 5.</summary>
        public static int Combine(int areaRating, bool danceFloorNearby)
        {
            return PerformanceMath.ClampVenue(Math.Max(0, areaRating) + (danceFloorNearby ? DanceFloorBonus : 0));
        }

        public static int GetRating(uint performer)
        {
            return Combine(GetAreaRating(GetArea(performer)), HasDanceFloorNearby(performer));
        }

        public static int GetAreaRating(uint area)
        {
            if (!GetIsObjectValid(area))
                return 0;

            var now = DateTime.UtcNow;
            if (_areaRatings.TryGetValue(area, out var cached) && cached.ExpiresUtc > now)
                return cached.Rating;

            var rating = PerformanceMath.ClampVenue(ReadAreaRating(area));
            ForgetExpiredAreaRatings(now);
            _areaRatings[area] = (rating, now + _areaRatingLifetime);
            return rating;
        }

        // Instance areas such as cantina interiors come and go, so lapsed entries are dropped as the cache grows.
        private static void ForgetExpiredAreaRatings(DateTime now)
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

        public static bool HasDanceFloorNearby(uint performer)
        {
            var resref = GetDanceFloorResref();
            foreach (var placeable in ObjectSnapshot.InShape(
                         Shape.Sphere, DanceFloorRadiusMeters, GetLocation(performer), false, ObjectType.Placeable))
            {
                if (GetResRef(placeable) == resref)
                    return true;
            }

            return false;
        }

        private static int ReadAreaRating(uint area)
        {
            var authored = GetLocalInt(area, AreaRatingVariable);
            if (authored > 0)
                return authored;

            return GetPlayerCantinaRating(area);
        }

        // Interior area -> its property -> the cantina building -> the city whose Cantina level is the rating.
        private static int GetPlayerCantinaRating(uint area)
        {
            var propertyId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
                return 0;

            var interior = DB.Get<WorldProperty>(propertyId);
            if (interior == null ||
                interior.PropertyType != PropertyType.Cantina ||
                string.IsNullOrWhiteSpace(interior.ParentPropertyId))
                return 0;

            var building = DB.Get<WorldProperty>(interior.ParentPropertyId);
            if (building == null || string.IsNullOrWhiteSpace(building.ParentPropertyId))
                return 0;

            return Property.GetEffectiveUpgradeLevel(building.ParentPropertyId, PropertyUpgradeType.CantinaLevel);
        }

        private static string GetDanceFloorResref()
        {
            return _danceFloorResref ??= Property.GetStructureByType(StructureType.DanceFloor).Resref;
        }
    }
}
