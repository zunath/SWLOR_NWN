using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private state and the private cleanup of the server's property classes, which the engine tests used to
    /// read from the static <c>Property</c> class by reflection before the property system was converted. The layout
    /// entrances belong to the definitions catalog, the instance registry to the loader and the boot cleanup to the
    /// property service.
    /// </summary>
    public static class PropertyCaches
    {
        /// <summary>The entrance of every layout, as the catalog cached it. Changes show to the next entry.</summary>
        public static Dictionary<PropertyLayoutType, Vector4> EntrancesByLayout =>
            Field<Dictionary<PropertyLayoutType, Vector4>>(typeof(IPropertyCatalog), "_entrancesByLayout");

        /// <summary>The property instances the loader has registered, by property Id.</summary>
        public static Dictionary<string, PropertyInstance> Instances =>
            Field<Dictionary<string, PropertyInstance>>(typeof(IPropertyLoader), "_propertyInstances");

        /// <summary>Runs the clean-up the property service does at boot (expired leases, deletions, starship docks).</summary>
        public static void CleanUpData()
        {
            var service = CompositionRoot.ServerHost.Provider.GetService(typeof(IPropertyService));
            service.GetType().GetMethod("CleanUpData", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(service, null);
        }

        private static T Field<T>(Type contract, string name)
        {
            var instance = CompositionRoot.ServerHost.Provider.GetService(contract);
            return (T)instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }
    }
}
