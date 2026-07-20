using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// How a generated area behaves across server restarts.
    /// Only Ephemeral is implemented; the other modes are reserved so consumer code
    /// can declare intent now and pick up the behavior when those strategies land.
    /// </summary>
    public enum AreaPersistenceMode
    {
        /// <summary>Instance vanishes on restart; players who were inside relocate to the recorded entrance location.</summary>
        Ephemeral = 0,
        /// <summary>Seed and config persist; the same layout regenerates deterministically after a restart.</summary>
        SeedPersisted = 1,
        /// <summary>The realized area is exported as a real module resource and survives restarts.</summary>
        FullExport = 2
    }

    public class AreaGenerationRequest
    {
        public string TilesetResref { get; set; } = "tdt01";
        /// <summary>
        /// Key of the DungeonTilesetProfile actually composed for this request, when the caller
        /// resolved one via DungeonContentPlacer.GetComposition (see AreaGenerationChatCommand,
        /// SWLOR.ContentBuilder). Populate/PlaceDecorations reads this — falling back to the theme's
        /// own default TilesetProfileKey when empty — so a theme composed onto a NON-default tileset
        /// profile still dresses with THAT tileset's own palette instead of silently assuming the
        /// theme's default family (the root cause of a theme's decorations looking wrong when composed
        /// onto an unrelated tileset, e.g. Alien Ruin content on the Futuristic City tileset).
        /// </summary>
        public string TilesetProfileKey { get; set; } = string.Empty;
        /// <summary>Module area cloned as the shell for the generated grid. Must be registered in the module.</summary>
        public string PlaceholderResref { get; set; } = "gen_placeholder1";
        /// <summary>
        /// Terrain used for open space. Empty = the tileset's declared Floor terrain. Driven by
        /// DungeonTilesetProfile.PrimaryOpenTerrain for tilesets whose room vocabulary lives on a
        /// different terrain (zsf01 'floor', vmr01 'Plaza').
        /// </summary>
        public string OpenTerrainOverride { get; set; } = string.Empty;
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Fixed seed for deterministic output. Null picks a random seed per attempt.</summary>
        public int? Seed { get; set; }
        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;
        /// <summary>
        /// Bounded regeneration retries before the request fails cleanly. Path validation can
        /// reject a layout when a tileset's junction tiles have restricted path nodes (observed
        /// on zsf01, which has exactly one simple tile per corner class), so the budget must
        /// absorb several rejections.
        /// </summary>
        public int MaxAttempts { get; set; } = 6;
        public AreaPersistenceMode Persistence { get; set; } = AreaPersistenceMode.Ephemeral;
        public string DisplayName { get; set; } = "Generated Area";
        public string Tag { get; set; } = "GENERATED_AREA";
        /// <summary>Tile light color indices applied to every generated tile (theme-driven).</summary>
        public DungeonTileLighting Lighting { get; set; } = new();

        /// <summary>
        /// Resolved AREA atmosphere for this instance (see DungeonAreaAtmosphere), applied by
        /// AreaSynthesizer.Realize right after CreateArea. Callers composing through a tileset
        /// profile stamp composition.Tileset.ResolveAtmosphere(content.AtmosphereProfile) here,
        /// mirroring how Lighting travels. Null (the default) = keep the cloned placeholder's own
        /// area properties, exactly the pre-atmosphere behavior.
        /// </summary>
        public DungeonAreaAtmosphere Atmosphere { get; set; }

        /// <summary>
        /// Layout style/knobs, usually a theme's LayoutTemplate. Null falls back to
        /// RoomsAndCorridors with this request's MinRooms/MaxRooms. The facade clones it and
        /// stamps Width/Height/terrain labels, so the template itself is never mutated.
        /// </summary>
        public MacroLayoutParameters Layout { get; set; }

        /// <summary>
        /// Whether DungeonContentPlacer.Populate runs its placeable decoration pass (streetlights,
        /// planters, crates, wall clutter, ...) after ambient/boss/treasure content. Default true.
        /// </summary>
        public bool EnableDecorations { get; set; } = true;

        /// <summary>
        /// Decoration density as a percent of the theme's curated DungeonDetail.DecorationBaseDensity
        /// (0 = no decorations even when EnableDecorations is true; 100 = as-authored; up to 200 for a
        /// denser pass). See DungeonDecorationPlanner.Plan.
        /// </summary>
        public int DecorationDensityPercent { get; set; } = 100;

        /// <summary>
        /// Named decoration profile of the composed tileset to dress with (see
        /// DungeonTilesetProfile.DecorationProfiles -- e.g. fcx01's "ruined" destruction palette).
        /// Empty (the default) defers to the theme's own DungeonDetail.DecorationProfile declaration,
        /// which itself defaults to the tileset's standard palette. An unknown name falls back to the
        /// standard palette.
        /// </summary>
        public string DecorationProfile { get; set; } = string.Empty;
    }

    public class AreaGenerationResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public uint Area { get; set; } = OBJECT_INVALID;
        public ResolvedLayout Layout { get; set; }
        public int SeedUsed { get; set; }
        public int AttemptsUsed { get; set; }
    }
}
