using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over an .are (area) nwn_gff JSON document.</summary>
    public sealed class AreDocument : GffDocumentBase
    {
        public AreDocument(JsonGffDocument document) : base(document)
        {
        }

        public static AreDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static AreDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public string? Tileset
        {
            get => Root.GetStringOrNull("Tileset");
            set => Root.SetString("Tileset", GffFieldType.ResRef, value ?? string.Empty);
        }

        public int? Width
        {
            get => Root.GetIntOrNull("Width");
            set => Root.SetInt("Width", GffFieldType.Int, value ?? 0);
        }

        public int? Height
        {
            get => Root.GetIntOrNull("Height");
            set => Root.SetInt("Height", GffFieldType.Int, value ?? 0);
        }

        public string? Tag
        {
            get => Root.GetStringOrNull("Tag");
            set => Root.SetString("Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        /// <summary>The area's display name (locstring first-entry / language-0 text).</summary>
        public LocString Name => Root.GetOrAddLocString("Name");

        public uint? Flags
        {
            get => Root.GetUIntOrNull("Flags");
            set => Root.SetUInt("Flags", GffFieldType.Dword, value ?? 0);
        }

        /// <summary>The area's tile grid ("Tile_List"), each entry a Tile_ID/orientation/etc struct.</summary>
        public IReadOnlyList<JsonGffStruct> Tiles => Root.GetListOrEmpty("Tile_List");

        public float? FogClipDist
        {
            get => Root.GetSingleOrNull("FogClipDist");
            set => Root.SetSingle("FogClipDist", value ?? 0f);
        }

        public uint? SunAmbientColor
        {
            get => Root.GetUIntOrNull("SunAmbientColor");
            set => Root.SetUInt("SunAmbientColor", GffFieldType.Dword, value ?? 0);
        }

        public uint? SunDiffuseColor
        {
            get => Root.GetUIntOrNull("SunDiffuseColor");
            set => Root.SetUInt("SunDiffuseColor", GffFieldType.Dword, value ?? 0);
        }

        /// <summary>Packed fog colour used by day; see <see cref="SunAmbientColor"/> for the format.</summary>
        public uint? SunFogColor
        {
            get => Root.TryGet("SunFogColor", out var f) ? (uint)f.GetInteger() : null;
        }

        /// <summary>Packed fog colour used at night.</summary>
        public uint? MoonFogColor
        {
            get => Root.TryGet("MoonFogColor", out var f) ? (uint)f.GetInteger() : null;
        }

        public int? SunFogAmount
        {
            get => Root.GetIntOrNull("SunFogAmount");
            set => Root.SetInt("SunFogAmount", GffFieldType.Byte, value ?? 0);
        }

        public uint? MoonAmbientColor
        {
            get => Root.GetUIntOrNull("MoonAmbientColor");
            set => Root.SetUInt("MoonAmbientColor", GffFieldType.Dword, value ?? 0);
        }

        public uint? MoonDiffuseColor
        {
            get => Root.GetUIntOrNull("MoonDiffuseColor");
            set => Root.SetUInt("MoonDiffuseColor", GffFieldType.Dword, value ?? 0);
        }

        public int? MoonFogAmount
        {
            get => Root.GetIntOrNull("MoonFogAmount");
            set => Root.SetInt("MoonFogAmount", GffFieldType.Byte, value ?? 0);
        }

        public int? SkyBox
        {
            get => Root.GetIntOrNull("SkyBox");
            set => Root.SetInt("SkyBox", GffFieldType.Byte, value ?? 0);
        }

        public int? LightingScheme
        {
            get => Root.GetIntOrNull("LightingScheme");
            set => Root.SetInt("LightingScheme", GffFieldType.Byte, value ?? 0);
        }

        public int? DayNightCycle
        {
            get => Root.GetIntOrNull("DayNightCycle");
            set => Root.SetInt("DayNightCycle", GffFieldType.Byte, value ?? 0);
        }

        public bool? IsNight
        {
            get => Root.GetIntOrNull("IsNight") is { } value ? value != 0 : null;
            set => Root.SetInt("IsNight", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public int? ChanceRain
        {
            get => Root.GetIntOrNull("ChanceRain");
            set => Root.SetInt("ChanceRain", GffFieldType.Int, value ?? 0);
        }

        public int? ChanceSnow
        {
            get => Root.GetIntOrNull("ChanceSnow");
            set => Root.SetInt("ChanceSnow", GffFieldType.Int, value ?? 0);
        }

        public int? ChanceLightning
        {
            get => Root.GetIntOrNull("ChanceLightning");
            set => Root.SetInt("ChanceLightning", GffFieldType.Int, value ?? 0);
        }

        public int? WindPower
        {
            get => Root.GetIntOrNull("WindPower");
            set => Root.SetInt("WindPower", GffFieldType.Int, value ?? 0);
        }
    }
}
