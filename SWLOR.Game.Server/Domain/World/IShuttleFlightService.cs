using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The flights in the air and their shuttle interiors. Static callers reach the container's instance through
    /// <c>GameServices.ShuttleFlight</c>.
    /// </summary>
    public interface IShuttleFlightService
    {
        /// <summary>
        /// Returns the tracked flight for the given id, creating it and its instance if needed.
        /// </summary>
        ShuttleActiveFlight GetOrCreateFlight(string flightId, PlanetType origin, PlanetType destination, DateTime departureUtc, DateTime arrivalUtc);

        /// <summary>
        /// Gives a flight its live shuttle interior back: the area, the status console and the boarding point that
        /// the flight instance found or made in it.
        /// </summary>
        void AttachFlightInstance(ShuttleActiveFlight flight, uint area);

        /// <summary>
        /// Delivers any stragglers and destroys the shuttle instance, retrying until it succeeds.
        /// </summary>
        void DestroyFlightInstance(uint area, Location landingLocation);

        /// <summary>
        /// Retrieves the flight registered to a shuttle status console or shuttle area, if any.
        /// </summary>
        /// <param name="obj">The console placeable or area to check.</param>
        /// <param name="destination">The flight's destination planet.</param>
        /// <param name="arrivalUtc">The flight's UTC arrival time.</param>
        /// <returns>true if a flight was found, false otherwise.</returns>
        bool TryGetFlightInfo(uint obj, out PlanetType destination, out DateTime arrivalUtc);
    }
}
