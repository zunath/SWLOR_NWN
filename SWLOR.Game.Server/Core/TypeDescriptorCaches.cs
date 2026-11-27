using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The types System.ComponentModel's <see cref="TypeDescriptor"/> keeps for the life of the process. Newtonsoft.Json
    /// asks it for the converter of every type it builds a contract for, and it caches each type in tables of a framework
    /// assembly that every game build shares, so a retired build whose types are in them can never be unloaded. A build the
    /// host retires lets go of them (<see cref="IServerStaticState.ReleaseFrameworkTypeCaches"/>); the caches fill again on
    /// demand.
    /// </summary>
    /// <remarks>
    /// No public API empties these tables: <see cref="TypeDescriptor.Refresh(Type)"/> skips a type whose only cached data is
    /// its converter, and nothing removes a type from the default-provider tables. So after the framework's own hot reload
    /// handler has run, the tables are reached by their field names in the pinned runtime
    /// (<c>SWLOR.Game.Server/Docker/Dockerfile</c>); <c>TypeDescriptorCachesTests</c> and the many-reload test of
    /// SWLOR.Game.Host.Tests fail if a runtime renames them.
    /// </remarks>
    public static class TypeDescriptorCaches
    {
        private const BindingFlags StaticField = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags InstanceField = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// Clears the reflection caches the way the framework does for a hot reload (the <c>ClearCache</c> of the
        /// <see cref="MetadataUpdateHandlerAttribute"/> handlers of System.ComponentModel.TypeConverter), then removes every
        /// collectible type from <see cref="TypeDescriptor"/>'s per-type tables and from the per-type data of its
        /// reflection providers. Returns how many entries it removed from those tables.
        /// </summary>
        public static int ReleaseCollectibleTypes()
        {
            ClearReflectionCaches();

            var descriptor = typeof(TypeDescriptor);
            var defaultProviders = descriptor.GetField("s_defaultProviderInitialized", StaticField)?.GetValue(null) as IDictionary;
            var providerTypes = descriptor.GetField("s_providerTypeTable", StaticField)?.GetValue(null) as IDictionary;
            var providers = descriptor.GetField("s_providerTable", StaticField)?.GetValue(null) as IDictionary;
            var gate = descriptor.GetField("s_commonSyncObject", StaticField)?.GetValue(null) ?? descriptor;

            lock (gate)
            {
                var nodes = Values(providers).Concat(Values(providerTypes)).ToList();
                var released = RemoveCollectibleKeys(defaultProviders) + RemoveCollectibleKeys(providerTypes);
                foreach (var typeData in nodes.SelectMany(NodeChain).Select(TypeData).Distinct())
                    released += RemoveCollectibleKeys(typeData);
                return released;
            }
        }

        private static void ClearReflectionCaches()
        {
            foreach (var handler in typeof(TypeDescriptor).Assembly.GetCustomAttributes<MetadataUpdateHandlerAttribute>())
            {
                var clearCache = handler.HandlerType.GetMethod(
                    "ClearCache",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(Type[]) },
                    null);
                clearCache?.Invoke(null, new object[] { null });
            }
        }

        private static IEnumerable<object> Values(IDictionary table)
        {
            return table == null ? Enumerable.Empty<object>() : table.Values.Cast<object>().Where(value => value != null).ToList();
        }

        // A TypeDescriptor node and the nodes it chains to (TypeDescriptionNode.Next).
        private static IEnumerable<object> NodeChain(object node)
        {
            for (var current = node; current != null; current = current.GetType().GetField("Next", InstanceField)?.GetValue(current))
                yield return current;
        }

        // The per-type data of the node's provider (ReflectTypeDescriptionProvider._typeData), or null for another provider.
        private static IDictionary TypeData(object node)
        {
            var provider = node.GetType().GetField("Provider", InstanceField)?.GetValue(node);
            return provider?.GetType().GetField("_typeData", InstanceField)?.GetValue(provider) as IDictionary;
        }

        private static int RemoveCollectibleKeys(IDictionary table)
        {
            if (table == null)
                return 0;

            var collectible = table.Keys.OfType<Type>().Where(type => type.IsCollectible).ToList();
            foreach (var type in collectible)
                table.Remove(type);
            return collectible.Count;
        }
    }
}
