using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Remembers ticket holders who missed a departure so boarding calls and missed-shuttle
    /// messages are not repeated for every later departure while they stay away from the terminal.
    /// </summary>
    public class ShuttleMissedBoardingTracker
    {
        private readonly HashSet<string> _missed = new();

        /// <summary>
        /// Records a missed departure. Returns true only for the first miss, when the player should be told.
        /// </summary>
        public bool RecordMiss(string playerId)
        {
            return _missed.Add(playerId);
        }

        /// <summary>
        /// Returns true if the player missed a departure and has not returned to the terminal since.
        /// </summary>
        public bool HasMissed(string playerId)
        {
            return _missed.Contains(playerId);
        }

        /// <summary>
        /// Re-arms boarding calls and missed-shuttle messages for the player.
        /// </summary>
        public void Clear(string playerId)
        {
            _missed.Remove(playerId);
        }

        /// <summary>
        /// Returns the missed players among the given ids.
        /// </summary>
        public IReadOnlyCollection<string> GetMissed()
        {
            return new List<string>(_missed);
        }
    }
}
