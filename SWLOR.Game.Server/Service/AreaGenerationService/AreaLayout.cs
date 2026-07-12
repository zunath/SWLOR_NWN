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

        public MacroLayout(CornerTerrainGrid corners)
        {
            Corners = corners;
        }
    }

    public class MacroLayoutParameters
    {
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Terrain label for solid/unwalkable space (typically TilesetModel.DefaultTerrain).</summary>
        public string SolidTerrain { get; set; } = string.Empty;
        /// <summary>Terrain label for open/walkable space (typically TilesetModel.FloorTerrain).</summary>
        public string OpenTerrain { get; set; } = string.Empty;
        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;
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

        public ResolvedTile GetTile(int x, int y)
        {
            return Tiles[y * Width + x];
        }
    }
}
