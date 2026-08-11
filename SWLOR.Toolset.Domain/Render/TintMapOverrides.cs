using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Reads the tint-map locals from a blueprint without exposing raw VarTable plumbing.</summary>
    public static class TintMapOverrides
    {
        public static IReadOnlyDictionary<string, int> Read(VarTable variables)
        {
            ArgumentNullException.ThrowIfNull(variables);

            return variables
                .Where(entry =>
                    entry.Type == VarTable.TypeInt &&
                    entry.IntValue is > 0 &&
                    (entry.Name.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal) ||
                     TintMapVariable.IsCreatureColorStateName(entry.Name) ||
                     TintMapVariable.IsItemGlobalColorStateName(entry.Name)))
                .ToDictionary(
                    entry => entry.Name,
                    entry => entry.IntValue!.Value,
                    StringComparer.Ordinal);
        }

        /// <summary>
        /// Combines an equipped mesh's material dyes with the creature's semantic skin, hair and
        /// tattoo colors. Those appearance layers remain creature-owned even when armor supplies
        /// the mesh that exposes them.
        /// </summary>
        public static IReadOnlyDictionary<string, int> MergeCreatureLayers(
            IReadOnlyDictionary<string, int>? creatureOverrides,
            IReadOnlyDictionary<string, int>? itemOverrides)
        {
            var merged = itemOverrides?
                .Where(pair =>
                    !TintMapVariable.IsCreatureColorStateName(pair.Key) &&
                    (!TintMapVariable.TryGetLayer(pair.Key, out var layer) ||
                     !TintMapVariable.IsCreatureColorLayer(layer)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal) ??
                new Dictionary<string, int>(StringComparer.Ordinal);
            if (creatureOverrides == null)
                return merged;

            foreach (var (key, value) in creatureOverrides)
            {
                if (TintMapVariable.IsCreatureColorStateName(key) ||
                    TintMapVariable.TryGetLayer(key, out var layer) &&
                    TintMapVariable.IsCreatureColorLayer(layer))
                {
                    merged[key] = value;
                }
            }

            return merged;
        }

        /// <summary>
        /// Resolves a material-specific override, falling back to the persisted semantic/global
        /// intent used by the server when a newly exposed material has no exact key yet.
        /// </summary>
        public static int GetMaterialColor(
            IReadOnlyDictionary<string, int>? overrides,
            string materialName,
            TintMapLayerType layer)
        {
            if (overrides == null)
                return 0;

            var materialKey = TintMapVariable.GetName(materialName, layer);
            if (overrides.TryGetValue(materialKey, out var saved))
                return saved;

            var stateKey = TintMapVariable.IsCreatureColorLayer(layer)
                ? TintMapVariable.GetCreatureColorStateName(layer)
                : TintMapVariable.GetItemGlobalColorStateName(layer);
            return overrides.TryGetValue(stateKey, out saved) ? saved : 0;
        }

        public static int GetMaterialColor(
            VarTable variables,
            string materialName,
            TintMapLayerType layer)
        {
            ArgumentNullException.ThrowIfNull(variables);

            var materialKey = TintMapVariable.GetName(materialName, layer);
            if (variables.GetInt(materialKey) is int saved)
                return saved;

            var stateKey = TintMapVariable.IsCreatureColorLayer(layer)
                ? TintMapVariable.GetCreatureColorStateName(layer)
                : TintMapVariable.GetItemGlobalColorStateName(layer);
            return variables.GetInt(stateKey) ?? 0;
        }
    }
}
