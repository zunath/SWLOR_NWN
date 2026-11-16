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

        public static IReadOnlyList<AstromechPaintZone> Zones { get; } = new[]
        {
            AstromechPaintZone.Primary,
            AstromechPaintZone.Secondary,
            AstromechPaintZone.Trim
        };

        public static IReadOnlyList<AstromechChassis> All { get; } = new[]
        {
            new AstromechChassis(AppearanceType.SWLORAstromechChassis1, "c_astro_01", "Dome Frame",
                new TintMapColor(226, 226, 220), new TintMapColor(38, 84, 168), new TintMapColor(148, 152, 158)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis2, "c_astro_02", "Utility Frame",
                new TintMapColor(196, 190, 176), new TintMapColor(176, 96, 32), new TintMapColor(120, 122, 126)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis3, "c_astro_03", "Survey Frame",
                new TintMapColor(92, 118, 96), new TintMapColor(210, 190, 110), new TintMapColor(112, 114, 118)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis4, "c_astro_04", "Patrol Frame",
                new TintMapColor(62, 64, 70), new TintMapColor(150, 30, 30), new TintMapColor(96, 98, 104)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis5, "c_astro_05", "Salvage Frame",
                new TintMapColor(150, 92, 52), new TintMapColor(70, 120, 120), new TintMapColor(104, 98, 90)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis6, "c_astro_06", "Service Frame",
                new TintMapColor(214, 214, 208), new TintMapColor(168, 40, 40), new TintMapColor(140, 144, 150)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis7, "c_astro_07", "Scout Frame",
                new TintMapColor(206, 200, 186), new TintMapColor(40, 110, 70), new TintMapColor(132, 134, 138)),
            new AstromechChassis(AppearanceType.SWLORAstromechChassis8, "c_astro_08", "Courier Frame",
                new TintMapColor(70, 96, 150), new TintMapColor(220, 200, 90), new TintMapColor(126, 128, 134))
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

        public static float ClampScale(float scale)
        {
            if (float.IsNaN(scale) || scale <= 0f)
                return 1.0f;
            return scale < MinimumScale ? MinimumScale : scale > MaximumScale ? MaximumScale : scale;
        }
    }
}
