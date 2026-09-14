#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>Output of the macro layout stage; input to the tile resolver.</summary>
    public class MacroLayout
    {
        public int Seed { get; set; }
        public CornerTerrainGrid Corners { get; set; }
        /// <summary>
        /// Edge-crosser plan, always non-null and sized off the tile grid (Corners.Width x
        /// Corners.Height — Corners itself is the (Width+1) x (Height+1) CORNER grid for that same
        /// tile grid). Allocated alongside Corners in the constructor so every construction site stays
        /// in sync automatically; blank (all "") until a layout style opts into emitting crossers.
        /// </summary>
        public EdgeCrosserGrid Crossers { get; set; }
        public List<LayoutRoom> Rooms { get; set; } = new();
        /// <summary>Entrance/exit anchor points, assigned by the shared post-pass.</summary>
        public List<TransitionPoint> Transitions { get; set; } = new();
        /// <summary>Tunnel segments carved in Tunnel corridor mode (empty otherwise).</summary>
        public List<TunnelLink> TunnelLinks { get; set; } = new();
        /// <summary>
        /// Carried from MacroLayoutParameters.DoorTransitions by MacroLayoutGenerator.Generate — the
        /// tile resolver doesn't otherwise see generation parameters, only the macro layout itself.
        /// </summary>
        public bool DoorTransitions { get; set; } = true;
        /// <summary>Effective open-terrain label, carried from parameters for downstream consumers.</summary>
        public string OpenTerrain { get; set; } = string.Empty;
        /// <summary>
        /// Effective secondary district terrain label (empty = no districts), carried from
        /// MacroLayoutParameters.SecondaryOpenTerrain by MacroLayoutGenerator.Generate.
        /// </summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;
        /// <summary>Carried from MacroLayoutParameters.FeatureDensity by MacroLayoutGenerator.Generate.</summary>
        public double FeatureDensity { get; set; } = 0.05;
        /// <summary>Carried from MacroLayoutParameters.FeatureTiles by MacroLayoutGenerator.Generate.</summary>
        public Dictionary<string, int> FeatureTiles { get; set; } = new();
        /// <summary>Carried from MacroLayoutParameters.SetPieces by MacroLayoutGenerator.Generate.</summary>
        public Dictionary<string, int> SetPieces { get; set; } = new();
        /// <summary>
        /// Cells LayoutGroupStamper has stamped verbatim with a specific (tileId, orientation, height)
        /// from a tileset group. TileResolver places these tiles directly, bypassing corner/edge
        /// candidate lookup entirely; TileDoorPlanner must never claim a pinned cell for a transition
        /// door. Height is the final Tile_Height the pinned tile is placed at -- always 0 for the flat
        /// group kinds (exit groups, corridor inserts/stubs), and the height-aware placementHeight
        /// (site's grid min minus the tile's own corner-height min, exactly TileResolver's own
        /// convention) for a relief piece stamped onto painted non-flat corners (see
        /// LayoutGroupStamper's ReliefPiece kind).
        /// </summary>
        public Dictionary<(int X, int Y), (int TileId, int Orientation, int Height)> PinnedTiles { get; set; } = new();

        /// <summary>
        /// Footprints (tile-coordinate lists, in stamp order) of every OpenSetPiece group
        /// LayoutGroupStamper.Stamp placed this generation -- populated by
        /// LayoutGroupStamper.CommitOpenSetPiece. Consumed by LayoutRoadCarver.CarveSpurs (which runs
        /// immediately after Stamp -- see MacroLayoutGenerator.Generate's ordering) to connect any
        /// building whose site didn't land road-adjacent to the street network, matching hand-built
        /// fcx01's building-fronts-the-street pattern (see LayoutGroupStamper.IsOpenSetPieceSiteValid's
        /// roadAdjacent preference). Always empty when the composition has no SetPieces configured, or
        /// no group ever classifies/places as OpenSetPiece -- fully back-compat for every non-city
        /// tileset.
        /// </summary>
        public List<List<(int X, int Y)>> StampedOpenSetPieceFootprints { get; set; } = new();

        /// <summary>
        /// Carried from MacroLayoutParameters.ExitGroups by MacroLayoutGenerator.Generate — themed
        /// 1x1 "exit" group names (e.g. tdt01 Exit01-03) in priority order, consumed by
        /// GroupExitPlanner inside TileResolver.TryResolve. Empty = no group-exit substitution for
        /// this tileset (e.g. zsf01/Facility).
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        /// <summary>
        /// Carried from MacroLayoutParameters.DoorSlotCrossers by MacroLayoutGenerator.Generate —
        /// crosser names (beyond the canonical Doorway/Bridge pair) TileResolver.TryResolve treats as
        /// door-implying for its crosser+door-slot admission gate (see TileResolver's class doc
        /// comment). Empty = no alternate door-slot vocabulary for this tileset (every tileset except
        /// one that renames its door-implying crosser entirely, e.g. Barrows/tbw01's "door_corridor").
        /// </summary>
        public List<string> DoorSlotCrossers { get; set; } = new();

        /// <summary>
        /// Carried from MacroLayoutParameters.ExcludedTiles by MacroLayoutGenerator.Generate --
        /// physical tile IDs TileResolver must never place for this composition (confirmed
        /// placeholder/stub art, see DungeonTilesetProfile.ExcludedTiles). Empty = no exclusions for
        /// this tileset (default; every tileset profile except twc03/fortinterior_legacy).
        /// </summary>
        public HashSet<int> ExcludedTiles { get; set; } = new();

        public MacroLayout(CornerTerrainGrid corners)
        {
            Corners = corners;
            Crossers = new EdgeCrosserGrid(corners.Width, corners.Height);
        }
    }
}
