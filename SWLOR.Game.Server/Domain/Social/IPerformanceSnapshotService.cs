using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Reads a live performing session into a snapshot: the performer's perk-granted Performance stats, their unlocks and
    /// their venue. A part of the Performance service; it has no static facade.
    /// </summary>
    public interface IPerformanceSnapshotService
    {
        /// <summary>The snapshot of a performer's session for one audience tick.</summary>
        PerformerSnapshot Build(PerformanceSession session, int venue, int songTrackId);
    }
}
