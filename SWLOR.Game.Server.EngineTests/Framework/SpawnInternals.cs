using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's spawn instance (the per-area spawn caches and
    /// <c>AdjustScripts</c>). The engine tests drive them directly, as they did on the static class before the spawn
    /// service was converted.
    /// </summary>
    public static class SpawnInternals
    {
        private static object Instance => CompositionRoot.ServerHost.Provider.GetService(typeof(ISpawnService));

        /// <summary>The value of a private field of the spawn instance (<c>_activeSpawnsByArea</c>, <c>_queuedSpawns</c>, ...).</summary>
        public static object Field(string name)
        {
            var instance = Instance;
            return instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }

        /// <summary>Calls a private instance method of the spawn service with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var instance = Instance;
            return instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(instance, arguments);
        }
    }
}
