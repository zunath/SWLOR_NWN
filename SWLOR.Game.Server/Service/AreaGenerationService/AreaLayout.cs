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

    public enum RoomRole
    {
        Entrance = 0,
        Standard = 1,
        Boss = 2
    }

    public enum TransitionKind
    {
        /// <summary>An arrival point: players enter the area here.</summary>
        Entrance = 0,
        /// <summary>An outbound link: an exit placeable/transition spawns here.</summary>
        Exit = 1
    }

    /// <summary>
    /// A point where the area connects to the outside world. Assigned by the shared layout
    /// post-pass to fully-open tiles in distinct rooms, spread apart by geodesic distance.
    /// The first Entrance is the primary arrival anchor.
    /// </summary>
    public class TransitionPoint
    {
        public TransitionKind Kind { get; set; }
        /// <summary>Tile the transition sits on — always fully open.</summary>
        public (int X, int Y) Tile { get; set; }
        /// <summary>Id of the LayoutRoom hosting this transition.</summary>
        public int RoomId { get; set; }
    }

    public class LayoutRoom
    {
        public int Id { get; set; }
        public RoomRole Role { get; set; }
        /// <summary>Tile coordinates of the room's representative center, used for spawn/objective placement and path validation.</summary>
        public (int X, int Y) CenterTile { get; set; }
        /// <summary>All tile coordinates belonging to this room's open space.</summary>
        public List<(int X, int Y)> Tiles { get; set; } = new();
    }

    /// <summary>Output of the macro layout stage; input to the tile resolver.</summary>
    public class MacroLayout
    {
        public int Seed { get; set; }
        public CornerTerrainGrid Corners { get; set; }
        public List<LayoutRoom> Rooms { get; set; } = new();
        /// <summary>Entrance/exit anchor points, assigned by the shared post-pass.</summary>
        public List<TransitionPoint> Transitions { get; set; } = new();

        public MacroLayout(CornerTerrainGrid corners)
        {
            Corners = corners;
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
        PackedRooms = 3
    }

    public class MacroLayoutParameters
    {
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Terrain label for solid/unwalkable space (typically TilesetModel.DefaultTerrain).</summary>
        public string SolidTerrain { get; set; } = string.Empty;
        /// <summary>Terrain label for open/walkable space (typically TilesetModel.FloorTerrain).</summary>
        public string OpenTerrain { get; set; } = string.Empty;

        public DungeonLayoutStyle Style { get; set; } = DungeonLayoutStyle.RoomsAndCorridors;

        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;
        /// <summary>Room rectangle bounds in corners (RoomsAndCorridors/Warren chambers/PackedRooms leaves).</summary>
        public int MinRoomCornerSize { get; set; } = 3;
        public int MaxRoomCornerSize { get; set; } = 7;

        /// <summary>Corridor width in corners. 1 = narrow tunnels, 2 = broad halls.</summary>
        public int CorridorWidth { get; set; } = 1;

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

        /// <summary>Arrival points assigned to rooms (1..3). The first is the primary anchor.</summary>
        public int EntranceCount { get; set; } = 1;
        /// <summary>Outbound exit points assigned to rooms (1..3). Exit placeables spawn at each.</summary>
        public int ExitCount { get; set; } = 1;

        public MacroLayoutParameters Clone()
        {
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

        public ResolvedTile GetTile(int x, int y)
        {
            return Tiles[y * Width + x];
        }
    }
}
