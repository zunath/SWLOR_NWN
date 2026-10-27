using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's shuttle instance (<c>RestoreTicketedSession</c>,
    /// <c>DeliverOfflinePassenger</c>, <c>TryGetRideLoginRedirect</c> and the boarding-eligibility tables). The engine
    /// tests drive them directly, as they did on the static class before the shuttle service was converted.
    /// </summary>
    public static class ShuttleInternals
    {
        private static object Instance => CompositionRoot.ServerHost.Provider.GetService(typeof(IShuttleService));

        /// <summary>The session boarding-eligibility times of the shuttle instance, by player id.</summary>
        public static Dictionary<string, DateTime> BoardingTimes => (Dictionary<string, DateTime>)Field("_boardingNotBeforeUtc");

        /// <summary>The ticket holders of the shuttle instance, by player id.</summary>
        public static Dictionary<string, (PlanetType Origin, PlanetType Destination)> TicketHolders =>
            (Dictionary<string, (PlanetType Origin, PlanetType Destination)>)Field("_ticketHolders");

        /// <summary>Calls a private instance method of the shuttle service with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var instance = Instance;
            return instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(instance, arguments);
        }

        private static object Field(string name)
        {
            var instance = Instance;
            return instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }
    }
}
