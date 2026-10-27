using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Runs the private cache methods of the server's party instance (<c>AddToParty</c>, <c>RemoveCreatureFromParty</c>).
    /// A headless test player has no client to accept an invitation, so the engine tests call the join and leave
    /// directly, as they did on the static class before the party service was converted.
    /// </summary>
    public static class PartyCaches
    {
        /// <summary>Calls a private instance method of the container's party service with the given arguments.</summary>
        public static void Invoke(string methodName, params object[] arguments)
        {
            var party = CompositionRoot.ServerHost.Provider.GetService(typeof(IPartyService));
            party.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(party, arguments);
        }
    }
}
