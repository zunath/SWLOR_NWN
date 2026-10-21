using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class HeadTintChannels
    {
        private static readonly TintMapLayerType[] Native =
        {
            TintMapLayerType.Skin, TintMapLayerType.Hair,
            TintMapLayerType.Tattoo1, TintMapLayerType.Tattoo2
        };
        private static readonly TintMapLayerType[] Accessories =
        {
            TintMapLayerType.Cloth1, TintMapLayerType.Cloth2,
            TintMapLayerType.Leather1, TintMapLayerType.Leather2,
            TintMapLayerType.Metal1, TintMapLayerType.Metal2
        };

        public static bool IsHead(TintMapMaterialSelection selection) =>
            !selection.UsesItemColors &&
            selection.ModelResref.IndexOf("_head", StringComparison.OrdinalIgnoreCase) == 4;

        public static IReadOnlyList<TintMapLayerType> GetLayers(IEnumerable<TintMapMaterialSelection> selections)
        {
            var detected = selections.Where(IsHead).SelectMany(s => s.Material.Layers).ToHashSet();
            return Native.Concat(Accessories.Where(detected.Contains)).ToArray();
        }

        public static int GetSelectedIndex(IReadOnlyList<TintMapLayerType> layers, TintMapLayerType selected)
        {
            for (var i = 0; i < layers.Count; i++)
                if (layers[i] == selected)
                    return i;
            return 0;
        }

        public static string GetLabel(TintMapLayerType layer) => layer switch
        {
            TintMapLayerType.Skin => "Skin Color",
            TintMapLayerType.Hair => "Hair Color",
            TintMapLayerType.Tattoo1 => "Tattoo 1 Color",
            TintMapLayerType.Tattoo2 => "Tattoo 2 Color",
            TintMapLayerType.Cloth1 => "Cloth 1",
            TintMapLayerType.Cloth2 => "Cloth 2",
            TintMapLayerType.Leather1 => "Leather 1",
            TintMapLayerType.Leather2 => "Leather 2",
            TintMapLayerType.Metal1 => "Metal 1",
            TintMapLayerType.Metal2 => "Metal 2",
            _ => throw new ArgumentOutOfRangeException(nameof(layer))
        };
    }
}
