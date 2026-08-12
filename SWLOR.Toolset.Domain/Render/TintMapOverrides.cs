using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;
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
                     TintMapVariable.IsItemGlobalColorStateName(entry.Name) ||
                     ArmorColorIndexCalculator.IsPerPartOverrideVariableName(entry.Name)))
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
            TintMapLayerType layer,
            AppearanceArmor armorPart = AppearanceArmor.Invalid)
        {
            if (overrides == null)
                return 0;

            var materialKey = TintMapVariable.GetName(materialName, layer);
            if (overrides.TryGetValue(materialKey, out var saved))
                return saved;

            if (HasExplicitPerPartPreset(overrides, armorPart, layer))
                return 0;

            var stateKey = TintMapVariable.IsCreatureColorLayer(layer)
                ? TintMapVariable.GetCreatureColorStateName(layer)
                : TintMapVariable.GetItemGlobalColorStateName(layer);
            return overrides.TryGetValue(stateKey, out saved) ? saved : 0;
        }

        public static int GetMaterialColor(
            VarTable variables,
            string materialName,
            TintMapLayerType layer,
            AppearanceArmor armorPart = AppearanceArmor.Invalid)
        {
            ArgumentNullException.ThrowIfNull(variables);

            var materialKey = TintMapVariable.GetName(materialName, layer);
            if (variables.GetInt(materialKey) is int saved)
                return saved;

            if (HasExplicitPerPartPreset(variables, armorPart, layer))
            {
                return 0;
            }

            var stateKey = TintMapVariable.IsCreatureColorLayer(layer)
                ? TintMapVariable.GetCreatureColorStateName(layer)
                : TintMapVariable.GetItemGlobalColorStateName(layer);
            return variables.GetInt(stateKey) ?? 0;
        }

        /// <summary>
        /// Returns whether this armor part explicitly selected its ordinary palette color instead
        /// of inheriting an item-wide RGB tint for the layer.
        /// </summary>
        public static bool HasExplicitPerPartPreset(
            VarTable variables,
            AppearanceArmor armorPart,
            TintMapLayerType layer)
        {
            ArgumentNullException.ThrowIfNull(variables);

            return armorPart != AppearanceArmor.Invalid &&
                   TryGetArmorColorChannel(layer, out var colorChannel) &&
                   variables.GetInt(
                       ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                           armorPart,
                           colorChannel)) is > 0;
        }

        private static bool HasExplicitPerPartPreset(
            IReadOnlyDictionary<string, int> overrides,
            AppearanceArmor armorPart,
            TintMapLayerType layer)
        {
            return armorPart != AppearanceArmor.Invalid &&
                   TryGetArmorColorChannel(layer, out var colorChannel) &&
                   overrides.TryGetValue(
                       ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                           armorPart,
                           colorChannel),
                       out var marker) &&
                   marker > 0;
        }

        private static bool TryGetArmorColorChannel(
            TintMapLayerType layer,
            out AppearanceArmorColor colorChannel)
        {
            colorChannel = layer switch
            {
                TintMapLayerType.Metal1 => AppearanceArmorColor.Metal1,
                TintMapLayerType.Metal2 => AppearanceArmorColor.Metal2,
                TintMapLayerType.Cloth1 => AppearanceArmorColor.Cloth1,
                TintMapLayerType.Cloth2 => AppearanceArmorColor.Cloth2,
                TintMapLayerType.Leather1 => AppearanceArmorColor.Leather1,
                TintMapLayerType.Leather2 => AppearanceArmorColor.Leather2,
                _ => AppearanceArmorColor.NumColors
            };
            return colorChannel != AppearanceArmorColor.NumColors;
        }
    }
}
