using System.Globalization;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Patches a resolved <see cref="DungeonAreaAtmosphere"/> into GFF-JSON .are text -- the
    /// offline half of the atmosphere system (SWLOR.ProcgenReview's EmitArea calls this on every
    /// emitted area whose composed tileset resolves an atmosphere; the runtime half is
    /// AreaSynthesizer's post-CreateArea application). Field-by-field first-occurrence replacement
    /// on the placeholder's own JSON text, matching the emitter's existing surgical-patch style
    /// (ReplaceFirstIntField) rather than a parse/rewrite round trip, so every untouched byte of
    /// the placeholder .are survives verbatim.
    ///
    /// This path can set the three fields the runtime cannot (SunShadows/MoonShadows,
    /// LightingScheme, LoadScreenID) because it writes the .are directly. LoadScreenID is only
    /// patched when the atmosphere carries a non-null LoadScreenId.
    /// </summary>
    public static class AreaAtmosphereAreWriter
    {
        /// <summary>
        /// Returns the .are JSON text with every atmosphere field patched. Null atmosphere returns
        /// the text unchanged.
        /// </summary>
        public static string Apply(string areJson, DungeonAreaAtmosphere atmosphere)
        {
            if (atmosphere == null)
                return areJson;

            areJson = ReplaceField(areJson, "SkyBox", "byte", atmosphere.SkyBox);
            areJson = ReplaceField(areJson, "DayNightCycle", "byte", atmosphere.DayNightCycle ? 1 : 0);
            areJson = ReplaceField(areJson, "IsNight", "byte", atmosphere.IsNight ? 1 : 0);
            areJson = ReplaceField(areJson, "SunAmbientColor", "dword", atmosphere.SunAmbientColor);
            areJson = ReplaceField(areJson, "SunDiffuseColor", "dword", atmosphere.SunDiffuseColor);
            areJson = ReplaceField(areJson, "MoonAmbientColor", "dword", atmosphere.MoonAmbientColor);
            areJson = ReplaceField(areJson, "MoonDiffuseColor", "dword", atmosphere.MoonDiffuseColor);
            areJson = ReplaceField(areJson, "SunFogAmount", "byte", atmosphere.SunFogAmount);
            areJson = ReplaceField(areJson, "SunFogColor", "dword", atmosphere.SunFogColor);
            areJson = ReplaceField(areJson, "MoonFogAmount", "byte", atmosphere.MoonFogAmount);
            areJson = ReplaceField(areJson, "MoonFogColor", "dword", atmosphere.MoonFogColor);
            areJson = ReplaceField(areJson, "SunShadows", "byte", atmosphere.SunShadows ? 1 : 0);
            areJson = ReplaceField(areJson, "MoonShadows", "byte", atmosphere.MoonShadows ? 1 : 0);
            areJson = ReplaceField(areJson, "ShadowOpacity", "byte", atmosphere.ShadowOpacity);
            areJson = ReplaceField(areJson, "WindPower", "int", atmosphere.WindPower);
            areJson = ReplaceField(areJson, "ChanceRain", "int", atmosphere.ChanceRain);
            areJson = ReplaceField(areJson, "ChanceSnow", "int", atmosphere.ChanceSnow);
            areJson = ReplaceField(areJson, "ChanceLightning", "int", atmosphere.ChanceLightning);
            areJson = ReplaceField(areJson, "LightingScheme", "byte", atmosphere.LightingScheme);
            areJson = ReplaceFloatField(areJson, "FogClipDist", atmosphere.FogClipDist);
            if (atmosphere.LoadScreenId.HasValue)
                areJson = ReplaceField(areJson, "LoadScreenID", "word", atmosphere.LoadScreenId.Value);

            return areJson;
        }

        private static string ReplaceField(string json, string field, string gffType, int value)
        {
            return new Regex($"(\"{field}\": \\{{\\s*\"type\": \"{gffType}\",\\s*\"value\": )-?\\d+")
                .Replace(json, "${1}" + value.ToString(CultureInfo.InvariantCulture), 1);
        }

        private static string ReplaceFloatField(string json, string field, float value)
        {
            // GFF-JSON floats always carry a decimal point (e.g. "65.0"); keep that lexeme shape.
            var text = value.ToString("0.0###", CultureInfo.InvariantCulture);
            return new Regex($"(\"{field}\": \\{{\\s*\"type\": \"float\",\\s*\"value\": )-?[0-9.eE+-]+")
                .Replace(json, "${1}" + text, 1);
        }
    }
}
