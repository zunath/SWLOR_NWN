using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Which song a musician plays: the one they picked in the Perform window when it is a Cantina song they own,
    /// otherwise the first Cantina song in their collection, or the starter Cantina song every player owns. A part of the
    /// Performance service; <c>GameServices.Performance</c> callers reach it through <see cref="IPerformanceService"/>.
    /// </summary>
    public interface IPerformanceSongService
    {
        /// <summary>The Cantina songs the player owns, plus the starter song every player owns, in catalog order.</summary>
        IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId);

        /// <summary>The song the musician plays: their pick when they own it, else the first collected Cantina song, else the starter.</summary>
        int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong);
    }
}
