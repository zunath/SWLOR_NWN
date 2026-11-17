using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Holds the shuttle flight state that was fields of <see cref="ShuttleService"/>, moved here unchanged (P5-9c) so the
    /// shuttle services that split from it share one owner of it.
    /// </summary>
    public class ShuttleStateService : IShuttleStateService
    {
        // The flights and tickets come back from Redis (the rerun LoadShuttleFlights) and the live shuttle interiors (AdoptFlights)
        // on a hot reload; the boarding calls and permissions belong to the players' session.
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, ShuttleActiveFlight> _activeFlights = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, (PlanetType Origin, PlanetType Destination)> _ticketHolders = new();
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.World.ShuttleService")]
        private readonly Dictionary<string, DateTime> _boardingNotBeforeUtc = new();
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.World.ShuttleService")]
        private readonly Dictionary<(PlanetType Origin, PlanetType Destination), DateTime> _announcedBoardings = new();
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.World.ShuttleService")]
        private readonly ShuttleMissedBoardingTracker _missedBoardings = new();
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.World.ShuttleService")]
        private readonly ShuttleBoardingTracker _boardings = new();

        public Dictionary<string, ShuttleActiveFlight> ActiveFlights => _activeFlights;

        public Dictionary<string, (PlanetType Origin, PlanetType Destination)> TicketHolders => _ticketHolders;

        public Dictionary<string, DateTime> BoardingNotBeforeUtc => _boardingNotBeforeUtc;

        public Dictionary<(PlanetType Origin, PlanetType Destination), DateTime> AnnouncedBoardings => _announcedBoardings;

        public ShuttleMissedBoardingTracker MissedBoardings => _missedBoardings;

        public ShuttleBoardingTracker Boardings => _boardings;
    }
}
