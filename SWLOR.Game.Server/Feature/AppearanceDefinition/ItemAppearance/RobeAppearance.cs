using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public static class RobeAppearance
    {
        public static IReadOnlyList<int> GetAvailableStyles(uint creature, IEnumerable<int> styles) =>
            FilterStyles(styles, style => TintMapModelResolver.GetCurrentRobeModelResref(creature, style),
                model => !string.IsNullOrEmpty(ResManGetAliasFor(model, ResType.MDL)));

        public static IReadOnlyList<int> FilterStyles(IEnumerable<int> styles,
            Func<int, string> getModel, Func<string, bool> modelExists) =>
            styles.Where(style => style == 0 || style > 0 &&
                !string.IsNullOrEmpty(getModel(style)) && modelExists(getModel(style)))
                .Distinct().ToArray();

        public static bool RemoveUnavailableRobe(uint creature, uint item)
        {
            if (GetItemInSlot(InventorySlot.Chest, creature) != item)
                return false;

            var style = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);
            if (style <= 0)
                return false;
            var model = TintMapModelResolver.GetCurrentRobeModelResref(creature, style);
            // A non-parts creature has no modular robe model to validate.
            if (string.IsNullOrEmpty(model) || !string.IsNullOrEmpty(ResManGetAliasFor(model, ResType.MDL)))
                return false;

            // Keep the armor, its other parts and all saved dyes. Missing robe geometry
            // must not hide the body through parts_robe's otherwise valid hiding flags.
            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
            return true;
        }
    }
}
