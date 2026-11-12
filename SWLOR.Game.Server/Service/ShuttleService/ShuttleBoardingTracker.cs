using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// Follows passengers from the moment they are booked onto a departing flight until they are
    /// actually standing in its shuttle. The boarding jump is a queued action that can be dropped,
    /// so a passenger still outside the shuttle after the retry interval is sent again.
    /// </summary>
    public class ShuttleBoardingTracker
    {
        /// <summary>
        /// How long a boarding jump may take (including the area load) before it is sent again.
        /// </summary>
        public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(6);

        private class PendingBoarding
        {
            public string FlightId { get; init; } = string.Empty;
            public DateTime LastAttemptUtc { get; set; }
            public int Attempts { get; set; }
        }

        private readonly Dictionary<string, PendingBoarding> _pending = new();

        /// <summary>
        /// Records that a boarding jump was sent to the passenger for the given flight.
        /// </summary>
        public void Start(string playerId, string flightId, DateTime now)
        {
            _pending[playerId] = new PendingBoarding
            {
                FlightId = flightId,
                LastAttemptUtc = now,
                Attempts = 1
            };
        }

        /// <summary>
        /// Stops following the passenger: they reached the shuttle, left the server, or the ride ended.
        /// </summary>
        public void Clear(string playerId)
        {
            _pending.Remove(playerId);
        }

        /// <summary>
        /// Returns true if the passenger is still on their way to the given flight's shuttle.
        /// </summary>
        public bool IsBoarding(string playerId, string flightId)
        {
            return _pending.TryGetValue(playerId, out var pending) && pending.FlightId == flightId;
        }

        /// <summary>
        /// Returns the passengers still outside their shuttle, with the flight each one is booked on.
        /// </summary>
        public IReadOnlyList<(string PlayerId, string FlightId)> GetPending()
        {
            return _pending.Select(x => (x.Key, x.Value.FlightId)).ToList();
        }

        /// <summary>
        /// Returns true and records a new attempt when the passenger's last boarding jump is older
        /// than the retry interval. The returned attempt number counts the original jump as 1.
        /// </summary>
        public bool TryStartRetry(string playerId, DateTime now, out int attempt)
        {
            attempt = 0;
            if (!_pending.TryGetValue(playerId, out var pending) ||
                now - pending.LastAttemptUtc < RetryInterval)
                return false;

            pending.LastAttemptUtc = now;
            pending.Attempts++;
            attempt = pending.Attempts;
            return true;
        }
    }
}
