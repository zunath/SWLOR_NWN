using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The venue rating of a performance, 0 to 5: the area's rating plus one for a Dance Floor within 3 meters.
    /// Open world is 0, an NPC cantina sets the PERFORMANCE_VENUE_RATING area variable, and a player cantina takes its
    /// city's Cantina level. Each point adds 1% skill XP, 10% banking and 12 minutes of Inspired cap.
    /// The logic lives in <see cref="IPerformanceVenueService"/>; every member here forwards to the container's instance.
    /// This is a further facade of the <c>Performance</c> service (<c>ConvertedServices.json</c>).
    /// </summary>
    public static class PerformanceVenue
    {
        public const string AreaRatingVariable = Domain.Social.PerformanceVenueService.AreaRatingVariable;
        public const float DanceFloorRadiusMeters = Domain.Social.PerformanceVenueService.DanceFloorRadiusMeters;
        public const int DanceFloorBonus = Domain.Social.PerformanceVenueService.DanceFloorBonus;

        private static IPerformanceVenueService Instance => FacadeAccessor.Resolve<IPerformanceVenueService>();

        /// <summary>Pure: the area's rating plus the Dance Floor point, held to 0 through 5.</summary>
        public static int Combine(int areaRating, bool danceFloorNearby) => Instance.Combine(areaRating, danceFloorNearby);

        /// <summary>The performer's venue rating: their area's rating plus the Dance Floor point.</summary>
        public static int GetRating(uint performer) => Instance.GetRating(performer);

        /// <summary>The rating and where it comes from, for the Perform window. Reads the Dance Floor once, so call it when a window opens.</summary>
        public static PerformanceVenueBreakdown GetBreakdown(uint performer) => Instance.GetBreakdown(performer);

        /// <summary>The area's rating, cached for a few minutes.</summary>
        public static int GetAreaRating(uint area) => Instance.GetAreaRating(area);

        /// <summary>True when a Dance Floor placeable is within its radius of the performer.</summary>
        public static bool HasDanceFloorNearby(uint performer) => Instance.HasDanceFloorNearby(performer);
    }
}
