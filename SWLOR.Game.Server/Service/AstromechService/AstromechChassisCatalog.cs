using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// Every chassis an astromech player can pick. The first entry is the one a new character starts in,
    /// and must match the appearance on the Astromech row of racialtypes.2da.
    /// </summary>
    public static class AstromechChassisCatalog
    {
        public const float MinimumScale = 0.85f;
        public const float MaximumScale = 1.15f;
        private const byte BrightestSafeBlue = 252;

        public static IReadOnlyList<AstromechPaintZone> Zones { get; } = new[]
        {
            AstromechPaintZone.Primary,
            AstromechPaintZone.Secondary,
            AstromechPaintZone.Trim
        };

        // The paints were picked in the client: the tint shaders light a color from its nearest palette
        // swatch, so a value has to be looked at before it is used (greys near 228 pick up a chrome swatch).
        public static IReadOnlyList<AstromechChassis> All { get; } = new[]
        {
            new AstromechChassis(AppearanceType.SWLORAstromechChassis1, "c_astro_01", "Dome Frame",
                new TintMapColor(210, 210, 205), new TintMapColor(40, 90, 170), new TintMapColor(80, 82, 88)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis2, "c_astro_02", "Utility Frame",
                new TintMapColor(165, 100, 45), new TintMapColor(60, 62, 66), new TintMapColor(90, 90, 95)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis3, "c_astro_03", "Survey Frame",
                new TintMapColor(60, 110, 70), new TintMapColor(205, 175, 60), new TintMapColor(80, 82, 88)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis4, "c_astro_04", "Patrol Frame",
                new TintMapColor(52, 54, 60), new TintMapColor(170, 30, 30), new TintMapColor(95, 97, 102)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis5, "c_astro_05", "Salvage Frame",
                new TintMapColor(125, 72, 40), new TintMapColor(50, 110, 110), new TintMapColor(85, 80, 74)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis6, "c_astro_06", "Service Frame",
                new TintMapColor(210, 210, 205), new TintMapColor(170, 35, 35), new TintMapColor(90, 92, 98)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis7, "c_astro_07", "Scout Frame",
                new TintMapColor(40, 90, 170), new TintMapColor(210, 210, 205), new TintMapColor(80, 82, 88)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis8, "c_astro_08", "Courier Frame",
                new TintMapColor(215, 175, 40), new TintMapColor(52, 54, 60), new TintMapColor(90, 92, 98))
        };

        public static AstromechChassis Default => All[0];

        public static bool IsChassis(AppearanceType appearance)
        {
            return All.Any(chassis => chassis.Appearance == appearance);
        }

        /// <summary>
        /// The chassis for an appearance, or the starting chassis when the appearance is not one.
        /// </summary>
        public static AstromechChassis Get(AppearanceType appearance)
        {
            return All.FirstOrDefault(chassis => chassis.Appearance == appearance) ?? Default;
        }

        /// <summary>
        /// The tint layer a paint zone colors on every chassis material.
        /// </summary>
        public static TintMapLayerType GetLayer(AstromechPaintZone zone)
        {
            return zone switch
            {
                AstromechPaintZone.Secondary => TintMapLayerType.Cloth1,
                AstromechPaintZone.Trim => TintMapLayerType.Metal2,
                _ => TintMapLayerType.Cloth2
            };
        }

        /// <summary>
        /// The creature variables that give a chassis its paint: the player's choice for each zone,
        /// or the paint the chassis comes with where they have not chosen.
        /// </summary>
        public static IReadOnlyDictionary<string, int> BuildPaintVariables(
            AstromechChassis chassis,
            IReadOnlyDictionary<AstromechPaintZone, int> chosen)
        {
            var variables = new Dictionary<string, int>();
            foreach (var zone in Zones)
            {
                var stored = 0;
                var hasChoice = chosen != null &&
                                chosen.TryGetValue(zone, out stored) &&
                                TintMapColor.TryFromStoredValue(stored, out _);
                variables[TintMapVariable.GetName(chassis.Model, GetLayer(zone))] =
                    hasChoice ? stored : chassis.GetDefaultPaint(zone).ToStoredValue();
            }

            return variables;
        }

        /// <summary>
        /// The color a paint choice is stored as. The two brightest whites round up past the top of the
        /// range the tint shaders decode and come out red, so they are stored one step below.
        /// </summary>
        public static TintMapColor NormalizePaint(TintMapColor color)
        {
            return color.Red == 255 && color.Green == 255 && color.Blue > BrightestSafeBlue
                ? new TintMapColor(255, 255, BrightestSafeBlue)
                : color;
        }

        public static float ClampScale(float scale)
        {
            if (float.IsNaN(scale) || scale <= 0f)
                return 1.0f;
            return scale < MinimumScale ? MinimumScale : scale > MaximumScale ? MaximumScale : scale;
        }
    }
}
