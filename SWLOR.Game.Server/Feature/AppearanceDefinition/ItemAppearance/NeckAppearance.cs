using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public static class NeckAppearance
    {
        public static string GetBodyModel(uint creature, bool canonical = false)
        {
            var appearance = (int)GetAppearanceType(creature);
            if (!Get2DAString("appearance", "MODELTYPE", appearance).StartsWith("P", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            var race = Get2DAString("appearance", "RACE", appearance);
            if (string.IsNullOrWhiteSpace(race) || race.Length != 1)
                return string.Empty;
            var gender = GetGender(creature) == Gender.Female ? 'f' : 'm';
            var phenotype = canonical ? RobeModelRenderer.GetBasePhenotype(creature) : (int)GetPhenoType(creature);
            return $"p{gender}{race.ToLowerInvariant()}{phenotype}";
        }

        public static string ResolveModel(string body, int style, Func<int, int> fallback, Func<string, bool> exists)
        {
            if (string.IsNullOrEmpty(body) || body.Length < 4 || style <= 0 ||
                !int.TryParse(body.AsSpan(3), out var phenotype))
                return string.Empty;
            var visited = new HashSet<int>();
            while (visited.Add(phenotype))
            {
                var model = $"{body.Substring(0, 3)}{phenotype}_neck{style:D3}";
                if (exists(model))
                    return model;
                phenotype = fallback(phenotype);
            }
            return string.Empty;
        }

        public static string GetModel(uint creature, int style) => ResolveModel(GetBodyModel(creature, true), style,
            phenotype => int.TryParse(Get2DAString("phenotype", "DefaultPhenoType", phenotype), out var value) ? value : 0,
            model => !string.IsNullOrEmpty(ResManGetAliasFor(model, ResType.MDL)));

        public static IReadOnlyList<int> GetAvailableStyles(uint creature, IEnumerable<int> styles) =>
            styles.Where(style => style == 0 || !string.IsNullOrEmpty(GetModel(creature, style))).Distinct().ToArray();

        public static bool RemoveUnavailableNeck(uint creature, uint item)
        {
            if (GetItemInSlot(InventorySlot.Chest, creature) != item || string.IsNullOrEmpty(GetBodyModel(creature)))
                return false;
            var style = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
            if (style <= 0 || !string.IsNullOrEmpty(GetModel(creature, style)))
                return false;
            // Zero follows the wearer's naked neck; keep every other part and dye.
            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck, 0);
            return true;
        }
    }
}
