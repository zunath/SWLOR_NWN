using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Boarding: the boarding calls, boarding the ticket holders at a terminal onto a departing flight and following them
    /// until they stand in the shuttle.
    /// </summary>
    public interface IShuttleBoardingService
    {
        /// <summary>
        /// Re-enables boarding calls and missed-shuttle messages for players who missed a departure
        /// and have since come back within range of a flights terminal for their route.
        /// </summary>
        void RearmReturnedPassengers();

        /// <summary>Boards passengers for every scheduled departure that occurred between the last tick and now.</summary>
        void ProcessDepartures(DateTime lastTickUtc, DateTime now);

        /// <summary>
        /// Checks that every passenger booked onto a departed flight is actually aboard and sends again those who are not.
        /// </summary>
        void ConfirmBoardings(DateTime now);

        /// <summary>Announces the boarding call to ticket holders shortly before each departure.</summary>
        void ProcessBoardingCalls(DateTime now);

        /// <summary>
        /// Determines whether the player is within boarding range of a flights terminal for the given origin planet.
        /// </summary>
        bool IsAtBoardingTerminal(uint player, PlanetType origin);
    }
}
