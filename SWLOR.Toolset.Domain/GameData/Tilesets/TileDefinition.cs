namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// One entry from a [TERRAIN TYPES] block: a named ground surface a tile corner can carry.
    /// </summary>
    public sealed record TerrainDefinition(string Name, int? StrRef, string? UnlocalizedName);

    /// <summary>
    /// One entry from a [CROSSER TYPES] block: a named feature (bridge, doorway, ramp, ...) a
    /// tile edge can carry between two terrains.
    /// </summary>
    public sealed record CrosserDefinition(string Name, int? StrRef, string? UnlocalizedName);

    /// <summary>
    /// One entry from a [PRIMARY RULES]/[SECONDARY RULES] block describing how the toolset
    /// auto-terrains a newly placed tile against its neighbors.
    /// </summary>
    public sealed record TileRuleDefinition(
        string Placed,
        int PlacedHeight,
        string Adjacent,
        int AdjacentHeight,
        string Changed,
        int ChangedHeight);

    /// <summary>
    /// A door placement on a tile, from a [TILEnDOORd] block.
    /// </summary>
    public sealed record TileDoorDefinition(int Type, double X, double Y, double Z, double Orientation);

    /// <summary>
    /// One entry from a [GROUPn] block: a named, pre-arranged rectangle of tiles (by index into
    /// the tileset's [TILES] list) offered together in the toolset palette.
    /// </summary>
    public sealed record TileGroupDefinition(
        string Name,
        int Rows,
        int Columns,
        int? StrRef,
        IReadOnlyList<int> TileIndices);

    /// <summary>
    /// One entry from the [TILES] block: a single placeable tile with its corner terrains, edge
    /// crossers, lighting/animation slots, and any doors it carries.
    ///
    /// Corner/edge naming matches the .set file's own vocabulary (Top/Left/Right/Bottom are the
    /// four edge crossers; TopLeft/TopRight/BottomLeft/BottomRight are the four corner terrains).
    /// Terrain/crosser values are the raw names from [TERRAINn]/[CROSSERn] ("" when an edge has no
    /// crosser); they are not resolved against the tileset's terrain/crosser lists here.
    /// </summary>
    public sealed class TileDefinition
    {
        public string Model { get; init; } = "";
        public string WalkMesh { get; init; } = "";

        public string TopLeft { get; init; } = "";
        public int TopLeftHeight { get; init; }
        public string TopRight { get; init; } = "";
        public int TopRightHeight { get; init; }
        public string BottomLeft { get; init; } = "";
        public int BottomLeftHeight { get; init; }
        public string BottomRight { get; init; } = "";
        public int BottomRightHeight { get; init; }

        public string Top { get; init; } = "";
        public string Right { get; init; } = "";
        public string Bottom { get; init; } = "";
        public string Left { get; init; } = "";

        public int MainLight1 { get; init; }
        public int MainLight2 { get; init; }
        public int SourceLight1 { get; init; }
        public int SourceLight2 { get; init; }

        public int AnimLoop1 { get; init; }
        public int AnimLoop2 { get; init; }
        public int AnimLoop3 { get; init; }

        public int Sounds { get; init; }
        public string PathNode { get; init; } = "";
        public double Orientation { get; init; }

        public string? VisibilityNode { get; init; }
        public double? VisibilityOrientation { get; init; }
        public string? DoorVisibilityNode { get; init; }
        public double? DoorVisibilityOrientation { get; init; }

        public string? ImageMap2D { get; init; }

        /// <summary>Rare per-tile override of the tileset-level [GRASS] Grass flag.</summary>
        public int? Grass { get; init; }

        /// <summary>
        /// The raw "Doors=" value from this tile's block. NWN toolset output is not always
        /// internally consistent here: some corpus files carry a nonsensical Doors count (even
        /// negative) for a tile with zero actual [TILEnDOORd] blocks. Use <see cref="Doors"/> for
        /// the actual door placements; do not trust this count.
        /// </summary>
        public int DoorsRaw { get; init; }

        /// <summary>
        /// The tile's actual door placements, discovered by scanning [TILEnDOOR0], [TILEnDOOR1],
        /// ... for as long as consecutive blocks exist. Deliberately independent of
        /// <see cref="DoorsRaw"/>, which is not reliable in the corpus (see its remarks).
        /// </summary>
        public IReadOnlyList<TileDoorDefinition> Doors { get; init; } = Array.Empty<TileDoorDefinition>();
    }
}
