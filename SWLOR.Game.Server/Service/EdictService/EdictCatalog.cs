using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// Every Edict, built once from the <see cref="IEdictListDefinition"/> lists. Holds no engine or database calls, so
    /// the rules and their tests can read it directly.
    /// </summary>
    public static class EdictCatalog
    {
        private static readonly Lazy<IReadOnlyDictionary<EdictType, EdictDetail>> _edicts = new(Load);

        /// <summary>Every Edict by type.</summary>
        public static IReadOnlyDictionary<EdictType, EdictDetail> All => _edicts.Value;

        /// <summary>Every Edict in type order.</summary>
        public static IReadOnlyList<EdictDetail> Ordered => All.Values.OrderBy(x => (int)x.Type).ToList();

        /// <summary>The Edict's detail, or null for a type that is not an Edict.</summary>
        public static EdictDetail Get(EdictType type)
        {
            return All.TryGetValue(type, out var detail) ? detail : null;
        }

        private static IReadOnlyDictionary<EdictType, EdictDetail> Load()
        {
            var edicts = new Dictionary<EdictType, EdictDetail>();
            var listTypes = typeof(IEdictListDefinition).Assembly.GetTypes()
                .Where(x => typeof(IEdictListDefinition).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

            foreach (var listType in listTypes)
            {
                var list = (IEdictListDefinition)Activator.CreateInstance(listType);
                foreach (var (type, detail) in list.BuildEdicts())
                {
                    if (!edicts.TryAdd(type, detail))
                        throw new InvalidOperationException($"Edict '{type}' is defined by more than one list.");
                }
            }

            return edicts;
        }
    }
}
