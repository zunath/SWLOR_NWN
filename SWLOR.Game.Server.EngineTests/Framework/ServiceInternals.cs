using System.Reflection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reads the private caches and runs the private methods of a converted service's instance in the server's container
    /// (the Mimicry witness and description caches, the recharge visuals of a player). The state moved from static fields of
    /// the old static class to the instance, so the engine tests reach it through the container the server built.
    /// </summary>
    public static class ServiceInternals
    {
        /// <summary>The value of a private instance field of the container's <typeparamref name="TService"/>.</summary>
        public static TField Field<TService, TField>(string fieldName)
        {
            var service = Resolve<TService>();
            return (TField)service.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(service);
        }

        /// <summary>Calls a private instance method of the container's <typeparamref name="TService"/>.</summary>
        public static void Invoke<TService>(string methodName, params object[] arguments)
        {
            var service = Resolve<TService>();
            service.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(service, arguments);
        }

        private static object Resolve<TService>() =>
            CompositionRoot.ServerHost.Provider.GetService(typeof(TService));
    }
}
