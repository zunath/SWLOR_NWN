using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// Names the inventory-icon textures for an item blueprint, in the order they should be tried.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An item has no world model worth previewing in a palette - Aurora shows its inventory icon, and
    /// so does this. NWN derives that icon's resource name from baseitems.2da's ItemClass and the item's
    /// own ModelPart/ArmorPart numbers; which pattern applies depends on the base item's ModelType.
    /// </para>
    /// <para>
    /// Every pattern below was confirmed against the SWLOR corpus (7,651 uti blueprints resolved
    /// against the base game plus 113 hak layers). The order matters: it runs from the most specific
    /// name to the base item's generic DefaultIcon, so an item whose exact part has no artwork still
    /// gets the right *kind* of picture rather than nothing.
    /// </para>
    /// <para>
    /// Two of these look wrong until you check the files. Helmets are ModelType 1 and their icons are
    /// PLTs named <c>ihelm_###</c>, but cloaks - also ModelType 1 - carry a size infix
    /// (<c>icloak_m_###</c>), so both spellings have to be tried. Armor (ModelType 3) does not follow
    /// its own ItemClass at all: <c>iAArCl_###</c> exists nowhere, and armor icons are the body-part
    /// icons <c>ipm_chest###</c> / <c>ipf_chest###</c> keyed on the item's ArmorPart_Torso. A uti has no
    /// gender, so the male set is tried first and the female set second; almost all torso numbers ship
    /// both.
    /// </para>
    /// </remarks>
    public static class ItemIconResolver
    {
        /// <summary>
        /// The candidate icons for <paramref name="root"/> (a uti root struct), most specific first.
        /// Returns an empty list when the item's base item cannot be resolved at all.
        /// </summary>
        public static IReadOnlyList<IconLayerStack> Resolve(JsonGffStruct root, Func<int, BaseItemIconRow?> baseItems)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(baseItems);

            var baseItem = root.GetIntOrNull("BaseItem") ?? -1;
            var row = baseItem < 0 ? null : baseItems(baseItem);
            if (row == null)
                return Array.Empty<IconLayerStack>();

            var stacks = new List<IconLayerStack>(4);
            var itemClass = row.ItemClass;

            if (!string.IsNullOrWhiteSpace(itemClass))
            {
                var part1 = ItemAppearanceValues.Read(root, "ModelPart1") ?? 0;
                var part2 = ItemAppearanceValues.Read(root, "ModelPart2") ?? 0;
                var part3 = ItemAppearanceValues.Read(root, "ModelPart3") ?? 0;
                var torso = ItemAppearanceValues.Read(root, "ArmorPart_Torso") ?? 0;

                switch (row.ModelType)
                {
                    case 0:
                        Add(stacks, $"i{itemClass}_{part1:D3}", "simple item icon");
                        Add(stacks, $"i{itemClass}{part1:D3}", "simple item icon (unseparated)");
                        break;

                    case 1:
                        Add(stacks, $"i{itemClass}_{part1:D3}", "part item icon");
                        Add(stacks, $"i{itemClass}_m_{part1:D3}", "part item icon (sized)");
                        break;

                    case 2:
                        // One stack, three layers: the composite weapon icon is drawn in the same
                        // bottom/middle/top order the model is assembled in.
                        stacks.Add(new IconLayerStack(
                            new[]
                            {
                                $"i{itemClass}_b_{part1:D3}",
                                $"i{itemClass}_m_{part2:D3}",
                                $"i{itemClass}_t_{part3:D3}"
                            },
                            "composite item icon"));
                        break;

                    case 3:
                        Add(stacks, $"ipm_chest{torso:D3}", "armor icon");
                        Add(stacks, $"ipf_chest{torso:D3}", "armor icon (female set)");
                        break;

                    default:
                        // Unknown/malformed ModelType: try every shape rather than giving up.
                        Add(stacks, $"i{itemClass}_{part1:D3}", "item icon");
                        Add(stacks, $"i{itemClass}_b_{part1:D3}", "item icon (composite base)");
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.DefaultIcon))
                Add(stacks, row.DefaultIcon, "base item's default icon");

            return stacks;
        }

        private static void Add(List<IconLayerStack> stacks, string resRef, string status) =>
            stacks.Add(new IconLayerStack(new[] { resRef }, status));
    }
}
