using System.Reflection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reads the private state of the server's Combat helper instances (the Power Glove and Twin Pistol equipment, the
    /// paired ranged traits, the Twin Pistol mastery). The engine tests inspect the cached pairs and trait states, as
    /// they did on the static classes before those became container services.
    /// </summary>
    public static class CombatHelperInternals
    {
        /// <summary>The value of a private instance field of the container's instance of <typeparamref name="TContract"/>.</summary>
        public static TField Field<TContract, TField>(string fieldName)
        {
            var service = CompositionRoot.ServerHost.Provider.GetService(typeof(TContract));
            return (TField)service.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(service);
        }
    }
}
