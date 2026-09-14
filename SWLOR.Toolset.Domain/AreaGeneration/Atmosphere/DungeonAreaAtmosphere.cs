#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Atmosphere
{
    /// <summary>
    /// AREA-level lighting/sky/fog/weather properties a generated area carries, mined from the
    /// hand-built exemplar areas of one tileset family (module .are evidence, >= 3 areas agreeing
    /// on the full core tuple -- see the per-family evidence citations on each declaring profile).
    /// Distinct from <see cref="DungeonTileLighting"/>, which is PER-TILE light color indices;
    /// this is the area's own .are-level atmosphere: skybox, day/night behavior, sun/moon
    /// ambient/diffuse colors, fog, shadows, wind, and weather chances.
    ///
    /// <see cref="Authoring.GeneratedAreaDocumentPopulator"/> writes these values into the generated
    /// ARE document before the area triplet is committed to the open module.
    /// </summary>
    public class DungeonAreaAtmosphere
    {
        /// <summary>Row index into skyboxes.2da (.are SkyBox byte; 0 = no skybox).</summary>
        public int SkyBox { get; set; }
        /// <summary>True = the area cycles day/night; false = it is locked to one phase (see <see cref="IsNight"/>).</summary>
        public bool DayNightCycle { get; set; }
        /// <summary>Locked phase when <see cref="DayNightCycle"/> is false: true = always night, false = always day.</summary>
        public bool IsNight { get; set; }
        public int SunAmbientColor { get; set; }
        public int SunDiffuseColor { get; set; }
        public int MoonAmbientColor { get; set; }
        public int MoonDiffuseColor { get; set; }
        public int SunFogAmount { get; set; }
        public int SunFogColor { get; set; }
        public int MoonFogAmount { get; set; }
        public int MoonFogColor { get; set; }
        /// <summary>Whether sunlight casts shadows.</summary>
        public bool SunShadows { get; set; }
        /// <summary>Whether moonlight casts shadows.</summary>
        public bool MoonShadows { get; set; }
        public int ShadowOpacity { get; set; } = 50;
        /// <summary>0 (none), 1 (light), or 2 (strong).</summary>
        public int WindPower { get; set; }
        public int ChanceRain { get; set; }
        public int ChanceSnow { get; set; }
        public int ChanceLightning { get; set; }
        /// <summary>Lighting-scheme row written to the ARE document.</summary>
        public int LightingScheme { get; set; }
        public float FogClipDist { get; set; } = 45f;
        /// <summary>
        /// Optional loadscreens.2da row. Deliberately nullable: only set when the family
        /// evidence agrees on a meaningful non-zero loadscreen (e.g. ttd01's Tatooine screen);
        /// null keeps the placeholder .are's value untouched.
        /// </summary>
        public int? LoadScreenId { get; set; }
    }
}
