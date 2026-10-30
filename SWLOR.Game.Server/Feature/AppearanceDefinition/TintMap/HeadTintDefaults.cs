using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class HeadTintDefaults
    {
        private const string Table = "headtintdefs";
        private static readonly Dictionary<string, int> Defaults = new(StringComparer.OrdinalIgnoreCase);

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            Defaults.Clear();
            for (var row = 0; row < Get2DARowCount(Table); row++)
            {
                var model = Get2DAString(Table, "MODEL", row);
                var material = Get2DAString(Table, "MATERIAL", row);
                if (string.IsNullOrWhiteSpace(model) ||
                    !Regex.IsMatch(model, @"^p[fm][heodz]0_head\d{3}$", RegexOptions.IgnoreCase) ||
                    string.IsNullOrWhiteSpace(material) || material.Length > 16 ||
                    !int.TryParse(Get2DAString(Table, "LAYER", row), out var value) ||
                    !Enum.IsDefined(typeof(TintMapLayerType), value) ||
                    TintMapVariable.IsCreatureColorLayer((TintMapLayerType)value) ||
                    !int.TryParse(Get2DAString(Table, "COLOR", row), out var color) ||
                    color < 0 || color >= TintMapMaterialRegistry.PaletteColorCount)
                    continue;

                var key = Key(model, material, (TintMapLayerType)value);
                if (!Defaults.TryAdd(key, color))
                    throw new InvalidOperationException("Duplicate authored head tint default: " + key);
            }
        }

        public static bool TryGetPaletteColor(TintMapMaterialSelection selection, TintMapLayerType layer,
            out int color)
        {
            color = 0;
            return HeadTintChannels.IsHead(selection) &&
                   Defaults.TryGetValue(Key(selection.ModelResref, selection.Material.Resref, layer), out color);
        }

        private static string Key(string model, string material, TintMapLayerType layer) =>
            model + "/" + material + "/" + (int)layer;
    }
}
