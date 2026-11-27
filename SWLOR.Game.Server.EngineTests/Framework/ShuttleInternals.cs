using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's shuttle services (<c>RestoreTicketedSession</c>,
    /// <c>DeliverOfflinePassenger</c>, <c>TryGetRideLoginRedirect</c> and the boarding-eligibility tables). The engine
    /// tests drive them directly, as they did on the static class before the shuttle service was converted.
    /// </summary>
    public static class ShuttleInternals
    {
        private static readonly Type[] ServiceTypes =
        {
            typeof(IShuttleService), typeof(IShuttleFlightService), typeof(IShuttlePassengerService), typeof(IShuttleTicketService),
            typeof(IShuttleBoardingService), typeof(IShuttleArrivalService)
        };

        private static object State => CompositionRoot.ServerHost.Provider.GetService(typeof(IShuttleStateService));

        /// <summary>The session boarding-eligibility times of the shuttle services, by player id.</summary>
        public static Dictionary<string, DateTime> BoardingTimes => (Dictionary<string, DateTime>)Field("_boardingNotBeforeUtc");

        /// <summary>The flights in the air (or being built) of the shuttle services, by flight id.</summary>
        public static System.Collections.IDictionary ActiveFlights => (System.Collections.IDictionary)Field("_activeFlights");

        /// <summary>The ticket holders of the shuttle services, by player id.</summary>
        public static Dictionary<string, (PlanetType Origin, PlanetType Destination)> TicketHolders =>
            (Dictionary<string, (PlanetType Origin, PlanetType Destination)>)Field("_ticketHolders");

        /// <summary>Calls a private instance method of the shuttle services with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            foreach (var serviceType in ServiceTypes)
            {
                var instance = CompositionRoot.ServerHost.Provider.GetService(serviceType);
                var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                    return method.Invoke(instance, arguments);
            }

            throw new MissingMethodException(nameof(ShuttleInternals), methodName);
        }

        private static object Field(string name)
        {
            var instance = State;
            return instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }
    }
}
