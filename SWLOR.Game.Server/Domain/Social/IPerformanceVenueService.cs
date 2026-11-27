using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The venue rating of a performance, 0 to 5: the area's rating plus one for a Dance Floor within 3 meters. The
    /// static callers reach the container's instance of this interface through <c>GameServices.PerformanceVenue</c>.
    /// </summary>
    public interface IPerformanceVenueService
    {
        /// <summary>Pure: the area's rating plus the Dance Floor point, held to 0 through 5.</summary>
        int Combine(int areaRating, bool danceFloorNearby);

        /// <summary>The performer's venue rating: their area's rating plus the Dance Floor point.</summary>
        int GetRating(uint performer);

        /// <summary>The rating and where it comes from, for the Perform window. Reads the Dance Floor once, so call it when a window opens.</summary>
        PerformanceVenueBreakdown GetBreakdown(uint performer);

        /// <summary>The area's rating, cached for a few minutes.</summary>
        int GetAreaRating(uint area);

        /// <summary>True when a Dance Floor placeable is within its radius of the performer.</summary>
        bool HasDanceFloorNearby(uint performer);
    }
}
