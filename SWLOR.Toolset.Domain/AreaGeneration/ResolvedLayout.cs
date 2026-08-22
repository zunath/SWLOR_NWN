#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Fully resolved tile grid ready for realization.
    /// Tiles has Width * Height entries, index = y * Width + x with (0,0) the bottom-left tile,
    /// matching the row-major, bottom-up ordering used by NWN area tile data.
    /// </summary>
    public class ResolvedLayout
    {
        public string TilesetResref { get; set; } = string.Empty;
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ResolvedTile[] Tiles { get; set; } = System.Array.Empty<ResolvedTile>();
        public List<LayoutRoom> Rooms { get; set; } = new();
        /// <summary>Entrance/exit anchor points carried through from the macro layout.</summary>
        public List<TransitionPoint> Transitions { get; set; } = new();
        /// <summary>Effective open-terrain label used by this layout (may differ from the tileset's declared Floor).</summary>
        public string OpenTerrain { get; set; } = string.Empty;
        /// <summary>Effective secondary district terrain label used by this layout (empty = no districts).</summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;
        /// <summary>
        /// The <see cref="MacroLayout.Crossers"/> grid, carried through unchanged by
        /// TileResolver.TryResolve, exposes LayoutRoadCarver's carved road-lane edges
        /// (and every other post-pass crosser: Fence, accent-channel Bridge, etc.) to downstream
        /// consumers that only ever see the resolved layout, not the macro one. DungeonDecorationPlanner
        /// reads this to anchor road-side decoration along a carved lane -- see
        /// DungeonDecorationPlanner.IsRoadAdjacent. Never null (MacroLayout always allocates one).
        /// </summary>
        public EdgeCrosserGrid Crossers { get; set; }

        /// <summary>
        /// Every tile cell stamped as part of an OpenSetPiece structure footprint, carried through
        /// from MacroLayout.StampedOpenSetPieceFootprints by TileResolver.TryResolve (the same
        /// carry-through convention as <see cref="Crossers"/>) so downstream consumers that only see
        /// the resolved layout can reason about stamped buildings. DungeonDecorationPlanner reads
        /// this to route building-frontage decoration into the StructureAdjacent bucket -- see
        /// DungeonDecorationPlanner.IsStructureAdjacent. Empty for every composition that never
        /// stamps an OpenSetPiece.
        /// </summary>
        public HashSet<(int X, int Y)> StampedStructureTiles { get; set; } = new();

        /// <summary>
        /// Every margin cell hosting a structural building PLACEABLE erected by
        /// BuildingFrontagePlanner (the promenade-family canyon mechanism: skyscraper placeables
        /// standing on the non-walkable margin, flush against open-cell boundaries). Rebuilt from
        /// scratch (assignment, never accumulation) on every DungeonDecorationPlanner.Plan call, so
        /// repeated planning stays deterministic and idempotent. Consumed alongside
        /// <see cref="StampedStructureTiles"/> by DungeonDecorationPlanner.IsStructureAdjacent /
        /// FlushStructureDirection, so WallFlush cargo and structure-frontage dressing anchor
        /// against placeable buildings exactly as against stamped tile buildings -- the hand-built
        /// evidence (pw_ar_narscorpd) stacks its flush cargo against swd_build* placeable bases.
        /// Deliberately NOT read by AssignDistrictFlavors: district identity derives from tile
        /// structures and road frontage only, so walling a commercial promenade with skyscrapers
        /// cannot skew it industrial. Empty for every non-frontage tileset.
        /// </summary>
        public HashSet<(int X, int Y)> PlaceableStructureCells { get; set; } = new();

        /// <summary>
        /// Every cell TileResolver's feature sprinkling actually replaced with a 1x1 feature group
        /// tile, mapped to the configured feature group's NAME (see
        /// DungeonTilesetProfile.FeatureTiles) -- the same carry-through convention as
        /// <see cref="Crossers"/>/<see cref="StampedStructureTiles"/>. DungeonDecorationPlanner
        /// reads this to dress area-marking feature tiles (a grass lawn patch, a fountain court)
        /// with a composed ensemble instead of leaving a bare zone marker -- see
        /// DungeonTilesetProfile.FeatureTileDressings. Empty for every composition without feature
        /// tiles (and for height-aware layouts, where feature sprinkling is disabled).
        /// </summary>
        public Dictionary<(int X, int Y), string> FeatureTileCells { get; set; } = new();

        /// <summary>
        /// The macro layout's corner-terrain plan, carried through unchanged by
        /// TileResolver.TryResolve (the same carry-through convention as <see cref="Crossers"/>) --
        /// the resolved tiles were selected to MATCH this plan corner-for-corner, so it is the
        /// authoritative per-corner terrain semantics of the finished grid without re-deriving them
        /// from tile IDs. BuildingFrontagePlanner reads this (with
        /// <see cref="DungeonTilesetProfile.ChasmTerrains"/>) to keep building footprints supported
        /// by real platform surface instead of hanging over a chasm -- see FrontageSupportRule.
        /// Null only for layouts constructed without the resolver (legacy tests); consumers must
        /// treat null as "no corner semantics available".
        /// </summary>
        public CornerTerrainGrid CornerTerrains { get; set; }

        /// <summary>
        /// The tileset's height-transition unit in meters ([GENERAL] Transition), carried through
        /// by TileResolver.TryResolve so plan-time consumers can convert a ResolvedTile.Height
        /// index into a world-space surface height (surface Z = Height * HeightTransition for a
        /// flat-cornered tile) without re-parsing the .set. 0 when unset (legacy layouts).
        /// </summary>
        public float HeightTransition { get; set; }

        public ResolvedTile GetTile(int x, int y)
        {
            return Tiles[y * Width + x];
        }
    }
}
