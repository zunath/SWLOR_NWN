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
                    entry.Name.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal))
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
            var hasCreatureLayers = creatureOverrides?.Keys.Any(key =>
                TintMapVariable.TryGetLayer(key, out var layer) &&
                TintMapVariable.IsCreatureColorLayer(layer)) == true;
            if (!hasCreatureLayers)
                return itemOverrides ?? new Dictionary<string, int>(StringComparer.Ordinal);

            var merged = itemOverrides?.ToDictionary(pair => pair.Key, pair => pair.Value,
                             StringComparer.Ordinal) ??
                         new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (key, value) in creatureOverrides!)
            {
                if (TintMapVariable.TryGetLayer(key, out var layer) &&
                    TintMapVariable.IsCreatureColorLayer(layer))
                {
                    merged[key] = value;
                }
            }

            return merged;
        }
    }
}
