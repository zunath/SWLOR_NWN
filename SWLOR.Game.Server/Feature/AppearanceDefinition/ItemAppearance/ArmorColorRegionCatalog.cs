using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// The colour swatches of the appearance editor: one per armor target and dye channel (20 targets by 6 channels).
    /// Each swatch is driven through four bindings named from its target and channel, for example
    /// <c>ChestCloth1Region</c>, <c>ChestCloth1Selected</c>, <c>ChestCloth1Tint</c> and <c>ChestCloth1Custom</c>; the
    /// window definition binds them by those names. The order of <see cref="Entries"/> is the order the editor publishes
    /// the swatches in when it opens.
    /// </summary>
    public static class ArmorColorRegionCatalog
    {
        public const int TextureColorsPerRow = 16;
        public const int ColorSize = 16; // 16x16 colors on the sprite sheet

        // The single neutral pixel is the existing inheritance indicator.
        private const int NeutralX = 247;
        private const int NeutralY = 55;

        public static readonly IReadOnlyList<AppearanceColorTarget> Targets = new[]
        {
            AppearanceColorTarget.Global,
            AppearanceColorTarget.LeftShoulder, AppearanceColorTarget.LeftBicep, AppearanceColorTarget.LeftForearm,
            AppearanceColorTarget.LeftHand, AppearanceColorTarget.LeftThigh, AppearanceColorTarget.LeftShin,
            AppearanceColorTarget.LeftFoot,
            AppearanceColorTarget.RightShoulder, AppearanceColorTarget.RightBicep, AppearanceColorTarget.RightForearm,
            AppearanceColorTarget.RightHand, AppearanceColorTarget.RightThigh, AppearanceColorTarget.RightShin,
            AppearanceColorTarget.RightFoot,
            AppearanceColorTarget.Neck, AppearanceColorTarget.Chest, AppearanceColorTarget.Belt,
            AppearanceColorTarget.Pelvis, AppearanceColorTarget.Robe
        };

        public static readonly IReadOnlyList<AppearanceArmorColor> Channels = new[]
        {
            AppearanceArmorColor.Leather1, AppearanceArmorColor.Leather2,
            AppearanceArmorColor.Cloth1, AppearanceArmorColor.Cloth2,
            AppearanceArmorColor.Metal1, AppearanceArmorColor.Metal2
        };

        public static readonly IReadOnlyList<ArmorColorRegion> Entries = Build();

        private static readonly Dictionary<(AppearanceColorTarget, AppearanceArmorColor), ArmorColorRegion> _byKey = Index();

        public static bool TryGet(AppearanceColorTarget target, AppearanceArmorColor channel, out ArmorColorRegion region) =>
            _byKey.TryGetValue((target, channel), out region);

        public static ArmorColorRegion Get(AppearanceColorTarget target, AppearanceArmorColor channel) => _byKey[(target, channel)];

        /// <summary>The "no colour" swatch: a fresh rectangle on the inheritance indicator pixel.</summary>
        public static GuiRectangle NeutralRegion() => new(NeutralX, NeutralY, 1, 1);

        /// <summary>The palette cell that holds <paramref name="colorId"/>, or the inheritance indicator when it has none.</summary>
        public static GuiRectangle BuildPaletteRegion(int colorId)
        {
            // Do not sample palette row zero (a real color) or substitute the global color for an unset part.
            if (colorId < 0 || colorId >= TintMapMaterialRegistry.PaletteColorCount)
                return NeutralRegion();
            var (x, y) = ColorIdToCoordinates(colorId);
            return new GuiRectangle(x * ColorSize, y * ColorSize, ColorSize, ColorSize);
        }

        public static (int, int) ColorIdToCoordinates(int colorId)
        {
            var x = colorId % TextureColorsPerRow;
            var y = colorId / TextureColorsPerRow;

            return (x, y);
        }

        /// <summary>The glow binding of a swatch, from the name of its region binding.</summary>
        public static string GetSelectionName(string regionName) =>
            regionName[..^"Region".Length] + "Selected";

        private static List<ArmorColorRegion> Build()
        {
            var entries = new List<ArmorColorRegion>(Targets.Count * Channels.Count);
            foreach (var target in Targets)
                foreach (var channel in Channels)
                    entries.Add(new ArmorColorRegion(target, channel));
            return entries;
        }

        private static Dictionary<(AppearanceColorTarget, AppearanceArmorColor), ArmorColorRegion> Index()
        {
            var index = new Dictionary<(AppearanceColorTarget, AppearanceArmorColor), ArmorColorRegion>();
            foreach (var entry in Entries)
                index[(entry.Target, entry.Channel)] = entry;
            return index;
        }
    }
}
