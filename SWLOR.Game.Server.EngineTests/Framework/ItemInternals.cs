using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's item instance (<c>EnsureItemAndContentsActivations</c>). The engine
    /// tests drive them directly, as they did on the static class before the item service was converted.
    /// </summary>
    public static class ItemInternals
    {
        /// <summary>Calls a private instance method of the container's item service with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var instance = CompositionRoot.ServerHost.Provider.GetService(typeof(IItemService));
            return instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(instance, arguments);
        }
    }
}
