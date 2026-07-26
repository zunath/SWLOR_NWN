namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Just the identifying fields of a tileset's [GENERAL] block, for labelling a tileset picker
    /// without parsing the whole file (see <see cref="SetFileParser.ParseHeader"/>).
    /// </summary>
    /// <param name="Name">The tileset's internal name, usually the ResRef in caps ("ZTD01").</param>
    /// <param name="UnlocalizedName">The human-readable name ("[CEP] Desert"), absent in some tilesets.</param>
    /// <param name="DisplayNameStrRef">Strref for a localized name, or -1 when not declared.</param>
    public sealed record TilesetHeader(string Name, string UnlocalizedName, int DisplayNameStrRef);

    /// <summary>
    /// The parsed contents of one NWN .set tileset file: [GENERAL]/[GRASS] properties plus the
    /// terrain, crosser, auto-terrain rule, tile, and group tables. Pure data - no rendering, no
    /// file watching, no resolution against haks/2das.
    /// </summary>
    public sealed class TilesetDefinition
    {
        // [GENERAL]
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public string Version { get; init; } = "";
        public bool Interior { get; init; }
        public bool HasHeightTransition { get; init; }
        public string EnvMap { get; init; } = "";
        public int Transition { get; init; }
        public string UnlocalizedName { get; init; } = "";
        public string Border { get; init; } = "";
        public string Default { get; init; } = "";
        public string Floor { get; init; } = "";

        /// <summary>Custom strref for the tileset's display name, or -1 when not overridden.</summary>
        public int DisplayName { get; init; } = -1;

        /// <summary>Toolset camera height override for the tile selector. Not present in every file.</summary>
        public int? SelectorHeight { get; init; }

        // [GRASS]
        public bool HasGrass { get; init; }
        public double? GrassDensity { get; init; }
        public double? GrassHeight { get; init; }
        public string? GrassTextureName { get; init; }
        public double? AmbientRed { get; init; }
        public double? AmbientGreen { get; init; }
        public double? AmbientBlue { get; init; }
        public double? DiffuseRed { get; init; }
        public double? DiffuseGreen { get; init; }
        public double? DiffuseBlue { get; init; }

        public IReadOnlyList<TerrainDefinition> Terrains { get; init; } = Array.Empty<TerrainDefinition>();
        public IReadOnlyList<CrosserDefinition> Crossers { get; init; } = Array.Empty<CrosserDefinition>();
        public IReadOnlyList<TileRuleDefinition> PrimaryRules { get; init; } = Array.Empty<TileRuleDefinition>();
        public IReadOnlyList<TileRuleDefinition> SecondaryRules { get; init; } = Array.Empty<TileRuleDefinition>();
        public IReadOnlyList<TileDefinition> Tiles { get; init; } = Array.Empty<TileDefinition>();
        public IReadOnlyList<TileGroupDefinition> Groups { get; init; } = Array.Empty<TileGroupDefinition>();

        public int TileCount => Tiles.Count;
    }
}
