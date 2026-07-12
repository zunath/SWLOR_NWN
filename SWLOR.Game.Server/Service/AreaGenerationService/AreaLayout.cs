using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Corner-granularity terrain plan produced by the macro layout stage.
    /// Labels is (Width+1) x (Height+1), indexed [x, y] with y = 0 at the south (bottom) edge,
    /// matching NWN tile indexing where tile index 0 is the bottom-left tile.
    /// A tile at (tx, ty) touches corners (tx, ty), (tx+1, ty), (tx, ty+1), (tx+1, ty+1).
    /// </summary>
    public class CornerTerrainGrid
    {
        public int Width { get; }
        public int Height { get; }
        public string[,] Labels { get; }

        public CornerTerrainGrid(int width, int height, string fillTerrain)
        {
            Width = width;
            Height = height;
            Labels = new string[width + 1, height + 1];

            for (var x = 0; x <= width; x++)
            {
                for (var y = 0; y <= height; y++)
                {
                    Labels[x, y] = fillTerrain;
                }
            }
        }
    }

    /// <summary>
    /// Edge-crosser plan for the edges BETWEEN tile cells (and on the map border) of a WxH tile grid.
    /// Complements <see cref="CornerTerrainGrid"/>: where Corners labels the 4 terrain corners a tile
    /// touches, this grid labels the crosser (Corridor, Doorway, Bridge, Fence, Alley, ... or "" for
    /// blank) that must agree between two tiles sharing that edge.
    ///
    /// Storage is split by axis so a shared edge is a single cell, never duplicated:
    /// - Vertical edges (a cell's Left/Right, i.e. edges between horizontally adjacent cells) are
    ///   (Width+1) x Height — index x in [0..Width], x=0 is the map's west border edge, x=Width is
    ///   the east border edge.
    /// - Horizontal edges (a cell's Top/Bottom, i.e. edges between vertically adjacent cells) are
    ///   Width x (Height+1) — index y in [0..Height], y=0 is the map's south border edge, y=Height is
    ///   the north border edge.
    /// Setting cell (x,y)'s Right edge writes the same storage cell as neighbor (x+1,y)'s Left edge.
    /// Everything initializes to "" (blank).
    /// </summary>
    public class EdgeCrosserGrid
    {
        public int Width { get; }
        public int Height { get; }

        // Vertical edges: [x, y] with x in [0..Width]. VerticalEdges[x, y] is cell (x, y)'s Left edge
        // and cell (x-1, y)'s Right edge.
        private readonly string[,] _vertical;

        // Horizontal edges: [x, y] with y in [0..Height]. HorizontalEdges[x, y] is cell (x, y)'s Bottom
        // edge and cell (x, y-1)'s Top edge.
        private readonly string[,] _horizontal;

        public EdgeCrosserGrid(int width, int height)
        {
            Width = width;
            Height = height;

            _vertical = new string[width + 1, height];
            _horizontal = new string[width, height + 1];

            for (var x = 0; x <= width; x++)
            for (var y = 0; y < height; y++)
                _vertical[x, y] = string.Empty;

            for (var x = 0; x < width; x++)
            for (var y = 0; y <= height; y++)
                _horizontal[x, y] = string.Empty;
        }

        public string GetEdge(int cellX, int cellY, int slot)
        {
            switch (slot)
            {
                case EdgeSlot.Left: return _vertical[cellX, cellY];
                case EdgeSlot.Right: return _vertical[cellX + 1, cellY];
                case EdgeSlot.Bottom: return _horizontal[cellX, cellY];
                case EdgeSlot.Top: return _horizontal[cellX, cellY + 1];
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "Expected an EdgeSlot value.");
            }
        }

        public void SetEdge(int cellX, int cellY, int slot, string crosser)
        {
            var value = crosser ?? string.Empty;
            switch (slot)
            {
                case EdgeSlot.Left: _vertical[cellX, cellY] = value; break;
                case EdgeSlot.Right: _vertical[cellX + 1, cellY] = value; break;
                case EdgeSlot.Bottom: _horizontal[cellX, cellY] = value; break;
                case EdgeSlot.Top: _horizontal[cellX, cellY + 1] = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "Expected an EdgeSlot value.");
            }
        }
    }

    public enum RoomRole
    {
        Entrance = 0,
        Standard = 1,
        Boss = 2
    }

    /// <summary>How a layout style realizes the connections between rooms.</summary>
    public enum CorridorMode
    {
        /// <summary>Corridors are open-terrain corner bands (walkable floor lanes).</summary>
        OpenLane = 0,
        /// <summary>Corridors are Corridor edge-crosser chains through solid cells (wall-embedded tunnels).</summary>
        Tunnel = 1
    }

    /// <summary>
    /// Which crosser vocabulary Tunnel-mode corridors carve. Corridor (default) is the classic
    /// wall-embedded facility tunnel: a Corridor-edge body chain entering rooms through Doorway-edge
    /// ports (see LayoutTunnelCarver). Alley reuses the identical port/BFS/chain mechanics but carves
    /// vmr01's exterior "alley" crosser instead -- verified offline against vmr01 .set data, a single
    /// crosser name serves both the tunnel body (TILE221, all-solid straight pair) AND the room-facing
    /// port (TILE210, Plaza-cornered with the crosser on the solid side) -- there is no separate
    /// Doorway-equivalent the way Corridor mode has. Ignored unless CorridorMode is Tunnel.
    /// </summary>
    public enum CorridorCrosserType
    {
        Corridor = 0,
        Alley = 1
    }

    /// <summary>
    /// A tunnel segment connecting two open regions through solid cells. Recorded by layout styles
    /// carving in Tunnel mode so geodesic passes (role assignment) can traverse connections that do
    /// not exist in the open-corner graph.
    /// </summary>
    public class TunnelLink
    {
        /// <summary>Open corner where the tunnel meets open space on one side.</summary>
        public (int X, int Y) CornerA { get; set; }
        /// <summary>Open corner where the tunnel meets open space on the other side.</summary>
        public (int X, int Y) CornerB { get; set; }
        /// <summary>Traversal cost in cells (>= 1).</summary>
        public int Length { get; set; }
    }

    public enum TransitionKind
    {
        /// <summary>An arrival point: players enter the area here.</summary>
        Entrance = 0,
        /// <summary>An outbound link: an exit placeable/transition spawns here.</summary>
        Exit = 1
    }

    /// <summary>
    /// How a transition is realized in the finished area. Door substitution is opportunistic and
    /// tileset-dependent (see TileDoorPlanner) — every transition starts out and may remain Placeable.
    /// </summary>
    public enum TransitionStyle
    {
        /// <summary>Realized as a placeable spawned on <see cref="TransitionPoint.Tile"/> (original behavior).</summary>
        Placeable = 0,
        /// <summary>Realized as a real tileset door embedded in the room's wall.</summary>
        Door = 1,
        /// <summary>
        /// Realized as a themed 1x1 tileset "exit" group tile (e.g. tdt01 Exit01-03) pinned into the
        /// room's wall, with a real door spawned in its door slot (see GroupExitPlanner). Exit-kind
        /// transitions only; reuses the same Door*/DoorCell world-transform fields as Door style.
        /// </summary>
        GroupExit = 2
    }

    /// <summary>
    /// A point where the area connects to the outside world. Assigned by the shared layout
    /// post-pass to fully-open tiles in distinct rooms, spread apart by geodesic distance.
    /// The first Entrance is the primary arrival anchor.
    /// </summary>
    public class TransitionPoint
    {
        public TransitionKind Kind { get; set; }
        /// <summary>
        /// Tile the transition sits on/arrives at — always fully open. For Door style this is the
        /// room-side walkable cell adjacent to the door, relocated from the original assignment by
        /// TileDoorPlanner; for Placeable style it is unchanged from the layout post-pass.
        /// </summary>
        public (int X, int Y) Tile { get; set; }
        /// <summary>Id of the LayoutRoom hosting this transition.</summary>
        public int RoomId { get; set; }

        /// <summary>
        /// How this transition is realized. Placeable unless TileDoorPlanner or GroupExitPlanner
        /// substitutes a door.
        /// </summary>
        public TransitionStyle Style { get; set; } = TransitionStyle.Placeable;
        /// <summary>
        /// Door style: the solid-side terminator cell now hosting the doorway wall tile. GroupExit
        /// style: the cell now pinned with the exit group's tile (no separate terminator — the group
        /// tile carries no crosser edges).
        /// </summary>
        public (int X, int Y) DoorCell { get; set; }
        /// <summary>Door/GroupExit style only: world-space X of the door object.</summary>
        public float DoorX { get; set; }
        /// <summary>Door/GroupExit style only: world-space Y of the door object.</summary>
        public float DoorY { get; set; }
        /// <summary>Door/GroupExit style only: world-space Z of the door object.</summary>
        public float DoorZ { get; set; }
        /// <summary>Door/GroupExit style only: world-space facing (degrees, normalized to (-180, 180]) of the door object.</summary>
        public float DoorOrientation { get; set; }
    }

    public class LayoutRoom
    {
        public int Id { get; set; }
        public RoomRole Role { get; set; }
        /// <summary>Tile coordinates of the room's representative center, used for spawn/objective placement and path validation.</summary>
        public (int X, int Y) CenterTile { get; set; }
        /// <summary>All tile coordinates belonging to this room's open space.</summary>
        public List<(int X, int Y)> Tiles { get; set; } = new();
        /// <summary>
        /// True for a WallRoom set piece registered by LayoutGroupStamper: a pre-designed multi-tile
        /// chunk whose interior is walkable via its own baked model walkmesh, not the abstract
        /// corner-terrain path graph (its Tiles are fully-solid corner cells and its pathnodes are
        /// often not 'A'). Content placement and path validation must skip these rooms.
        /// </summary>
        public bool IsSetPiece { get; set; }

        /// <summary>
        /// The terrain label this room's interior is carved from. Defaults to the layout's primary
        /// OpenTerrain; districted RoomsAndCorridors/Tunnel layouts may carve a room from
        /// MacroLayoutParameters.SecondaryOpenTerrain instead (see MacroLayoutParameters.SecondaryOpenTerrain).
        /// Always populated by every layout style's room-building path, so downstream consumers
        /// (LayoutGroupStamper's OpenSetPiece matching) can rely on it rather than assuming the
        /// layout's single OpenTerrain applies to every room.
        /// </summary>
        public string OpenTerrain { get; set; } = string.Empty;
    }

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
        /// Cells LayoutGroupStamper has stamped verbatim with a specific (tileId, orientation) from a
        /// tileset group. TileResolver places these tiles directly, bypassing corner/edge candidate
        /// lookup entirely; TileDoorPlanner must never claim a pinned cell for a transition door.
        /// </summary>
        public Dictionary<(int X, int Y), (int TileId, int Orientation)> PinnedTiles { get; set; } = new();

        /// <summary>
        /// Carried from MacroLayoutParameters.ExitGroups by MacroLayoutGenerator.Generate — themed
        /// 1x1 "exit" group names (e.g. tdt01 Exit01-03) in priority order, consumed by
        /// GroupExitPlanner inside TileResolver.TryResolve. Empty = no group-exit substitution for
        /// this tileset (e.g. zsf01/Facility).
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        public MacroLayout(CornerTerrainGrid corners)
        {
            Corners = corners;
            Crossers = new EdgeCrosserGrid(corners.Width, corners.Height);
        }
    }

    /// <summary>
    /// The overall shape a macro layout carves. Styles are modeled on hand-built SWLOR areas:
    /// organic caverns (Korriban caverns), dense corridor warrens (Veles sewers), and
    /// wall-sharing packed rooms (facility interiors).
    /// </summary>
    public enum DungeonLayoutStyle
    {
        /// <summary>Rectangular rooms joined by corridors, with optional loop connections.</summary>
        RoomsAndCorridors = 0,
        /// <summary>Cellular-automata caves: winding, blobby open space with nooks and pockets.</summary>
        OrganicCave = 1,
        /// <summary>Maze-like corridor network with small chambers and loops (sewer/undercity feel).</summary>
        Warren = 2,
        /// <summary>Space subdivided into rooms sharing walls, joined by door gaps (facility feel).</summary>
        PackedRooms = 3,
        /// <summary>Near-perfect maze of long winding 1-corridor-wide passages with a few small chambers at junctions.</summary>
        Labyrinth = 4
    }

    public class MacroLayoutParameters
    {
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Terrain label for solid/unwalkable space (typically TilesetModel.DefaultTerrain).</summary>
        public string SolidTerrain { get; set; } = string.Empty;
        /// <summary>Terrain label for open/walkable space (typically TilesetModel.FloorTerrain).</summary>
        public string OpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Optional second open-terrain label for multi-terrain districts: some rooms are carved as
        /// walled districts of this terrain instead of OpenTerrain, connected via Tunnel-mode
        /// corridors (see RoomsAndCorridorsLayout, LayoutTunnelCarver). Empty = no districts (default;
        /// fully back-compat, zero extra RNG draws). v1 scope: honored only when Style is
        /// RoomsAndCorridors and CorridorMode is Tunnel — OpenLane corridors carve straight bands from
        /// room center to room center and would repaint a secondary room's interior back to
        /// OpenTerrain, so this field is silently ignored outside Tunnel mode. Callers must verify the
        /// tileset has full (secondary, solid) corner coverage AND Doorway-junction tiles for the
        /// secondary terrain before enabling (see MultiTerrainDistrictTests).
        /// </summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Fraction of non-entrance rooms carved from SecondaryOpenTerrain instead of OpenTerrain when
        /// districts are active (room 0 — the first room a style places — always stays OpenTerrain, so
        /// the entrance is biased toward the primary district). Ignored when SecondaryOpenTerrain is
        /// empty. Default 0.35 mirrors AccentDensity's "roughly this share" convention.
        /// </summary>
        public double SecondaryRoomFraction { get; set; } = 0.35;

        public DungeonLayoutStyle Style { get; set; } = DungeonLayoutStyle.RoomsAndCorridors;

        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;
        /// <summary>Room rectangle bounds in corners (RoomsAndCorridors/Warren chambers/PackedRooms leaves).</summary>
        public int MinRoomCornerSize { get; set; } = 3;
        public int MaxRoomCornerSize { get; set; } = 7;

        /// <summary>Corridor width in corners. 1 = narrow tunnels, 2 = broad halls. OpenLane mode only.</summary>
        public int CorridorWidth { get; set; } = 1;

        /// <summary>
        /// How corridors are realized. OpenLane carves open-terrain corner bands (original behavior).
        /// Tunnel keeps the corners solid and lays Corridor edge crossers along the cell path instead,
        /// resolving to wall-embedded tunnel tiles with Doorway junctions where a tunnel meets open
        /// space — the way hand-built facility interiors are assembled. Tunnel passages are always
        /// exactly one tile wide and traverse via crosser edges, so CorridorWidth and pathnode-driven
        /// minimum opening widths do not apply to them.
        /// </summary>
        public CorridorMode CorridorMode { get; set; } = CorridorMode.OpenLane;

        /// <summary>
        /// Crosser vocabulary Tunnel-mode corridors carve. See <see cref="CorridorCrosserType"/>.
        /// Ignored unless CorridorMode is Tunnel; the default Corridor value is fully back-compat.
        /// </summary>
        public CorridorCrosserType CorridorCrosserType { get; set; } = CorridorCrosserType.Corridor;

        /// <summary>
        /// Fraction of additional connections carved beyond the spanning tree (0 = tree only).
        /// Loops make layouts feel like real areas instead of dead-end branches.
        /// </summary>
        public double LoopFactor { get; set; } = 0.25;

        /// <summary>OrganicCave: target fraction of interior corners that end up open.</summary>
        public double OpenFillTarget { get; set; } = 0.45;
        /// <summary>OrganicCave: cellular-automata smoothing passes.</summary>
        public int SmoothingPasses { get; set; } = 4;

        /// <summary>
        /// Optional third terrain painted as patches strictly inside open space (e.g. Water pools
        /// in caves, Pit channels in sewers). Empty = none. Callers must verify the tileset covers
        /// all (open, accent) corner combinations before enabling (see TileResolver coverage).
        /// </summary>
        public string AccentTerrain { get; set; } = string.Empty;
        /// <summary>Fraction of open corners converted to accent patches (0..~0.2).</summary>
        public double AccentDensity { get; set; } = 0.0;

        /// <summary>
        /// Number of linear accent-terrain channels (one-cell-wide bands, e.g. a Water/Pit "river")
        /// carved through open space, each crossed by exactly one real Bridge edge-crosser chain.
        /// 0 = none (default; back-compat). Requires AccentTerrain to be set (same field
        /// AccentDensity patches use) and the tileset to carry Bridge-edge vocabulary — callers must
        /// verify coverage before enabling (see LayoutAccentChannelCarver).
        /// </summary>
        public int AccentChannels { get; set; } = 0;

        /// <summary>
        /// Number of linear Fence edge-crosser lines carved through open room interiors (e.g. a
        /// fenced-off maintenance yard divider in tds01 Sewers, or a courtyard partition in vmr01).
        /// Unlike AccentChannels, a fence line never repaints corner terrain -- every corner on both
        /// sides stays this layout's own open terrain the whole time -- so it needs no TunnelLink, but
        /// that also means the shared corner-graph connectivity check can't see a Fence barrier either
        /// (both sides still read as plain open corners). LayoutFenceCarver instead runs its own
        /// cell-level tentative-commit/verify/revert check (mirroring LayoutAccentChannelCarver's own
        /// pattern) before keeping any run. 0 = none (default; back-compat). Requires the tileset to
        /// carry Fence-edge vocabulary for the current
        /// OpenTerrain -- LayoutFenceCarver probes TileResolver.HasCandidate at carve time and
        /// silently no-ops when absent (e.g. tdt01, zsf01), so this is safe to enable on any
        /// tileset/profile pairing without per-tileset configuration.
        /// </summary>
        public int FenceLines { get; set; } = 0;

        /// <summary>Arrival points assigned to rooms (1..3). The first is the primary anchor.</summary>
        public int EntranceCount { get; set; } = 1;
        /// <summary>Outbound exit points assigned to rooms (1..3). Exit placeables spawn at each.</summary>
        public int ExitCount { get; set; } = 1;

        /// <summary>
        /// When true (default), TileDoorPlanner opportunistically substitutes a real tileset door
        /// (room-side doorway tile + solid-side terminator tile) for each transition instead of a
        /// placeable. Falls back to Placeable per-transition when the tileset has no usable flat,
        /// ungrouped door-slot tiles for that spot (e.g. zsf01, which has none at all).
        /// </summary>
        public bool DoorTransitions { get; set; } = true;

        /// <summary>
        /// Fraction of eligible open cells (see TileResolver) that receive a rare decorative feature
        /// tile (treasure mounds, pillars, hot springs, ...) instead of a normal open tile.
        /// </summary>
        public double FeatureDensity { get; set; } = 0.05;

        /// <summary>
        /// Feature group name -> relative weight, usually stamped from DungeonTilesetProfile.FeatureTiles
        /// by DungeonComposition.BuildLayoutParameters. Empty = no feature sprinkling. TileResolver
        /// re-verifies each name's structural eligibility rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> FeatureTiles { get; set; } = new();

        /// <summary>
        /// Set-piece group name -> max instances per area, usually stamped from
        /// DungeonTilesetProfile.SetPieces by DungeonComposition.BuildLayoutParameters. Empty = no
        /// set-piece stamping. LayoutGroupStamper re-verifies each name's structural eligibility
        /// (shape, corners, crossers) rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> SetPieces { get; set; } = new();

        /// <summary>
        /// Themed 1x1 "exit" group names (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions, in priority order — usually stamped from
        /// DungeonTilesetProfile.ExitGroups by DungeonComposition.BuildLayoutParameters. Empty = no
        /// group-exit substitution for this tileset. GroupExitPlanner re-verifies each name's
        /// structural eligibility (1x1, flat, crosser-free, has a door slot) rather than trusting
        /// this list blindly.
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        public MacroLayoutParameters Clone()
        {
            // MemberwiseClone shares the FeatureTiles/SetPieces dictionary references with the
            // original rather than copying them. That's fine: callers only ever assign these from an
            // immutable tileset-profile dictionary and never mutate them post-construction.
            return (MacroLayoutParameters)MemberwiseClone();
        }
    }

    public class ResolvedTile
    {
        public int TileId { get; set; }
        public int Orientation { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Fully resolved tile grid ready for realization.
    /// Tiles has Width * Height entries, index = y * Width + x with (0,0) the bottom-left tile —
    /// the same row-major, bottom-up ordering SetTileJson and NWNX tile overrides use.
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

        public ResolvedTile GetTile(int x, int y)
        {
            return Tiles[y * Width + x];
        }
    }
}
