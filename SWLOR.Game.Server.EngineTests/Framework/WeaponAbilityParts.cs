using System;
using System.Reflection;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>The weapon ability parts, content singletons the engine tests resolve from the server's container.</summary>
    public static class WeaponAbilityParts
    {
        /// <summary>The part's singleton, as the server builds it.</summary>
        public static T Get<T>()
            where T : class
        {
            var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true);
            var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .MakeGenericMethod(typeof(T));
            try
            {
                return (T)resolve.Invoke(null, null);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }
    }
}
