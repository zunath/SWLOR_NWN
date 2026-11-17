using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The shuttle flight state the other shuttle services share: the flights in the air, the ticket holders, the session
    /// boarding permissions, the announced boarding calls and the trackers of passengers being boarded or delivered.
    /// </summary>
    public interface IShuttleStateService
    {
        /// <summary>The flights in the air (or being built), by flight id.</summary>
        Dictionary<string, ShuttleActiveFlight> ActiveFlights { get; }

        /// <summary>The route of each player holding an unboarded ticket, by player id.</summary>
        Dictionary<string, (PlanetType Origin, PlanetType Destination)> TicketHolders { get; }

        /// <summary>The earliest departure each ticket holder may board this session, by player id.</summary>
        Dictionary<string, DateTime> BoardingNotBeforeUtc { get; }

        /// <summary>The departure each route's boarding call was last announced for.</summary>
        Dictionary<(PlanetType Origin, PlanetType Destination), DateTime> AnnouncedBoardings { get; }

        /// <summary>The ticket holders who missed a departure and have not yet come back to a terminal.</summary>
        ShuttleMissedBoardingTracker MissedBoardings { get; }

        /// <summary>The passengers being followed until they stand in their shuttle or at the destination.</summary>
        ShuttleBoardingTracker Boardings { get; }
    }
}
