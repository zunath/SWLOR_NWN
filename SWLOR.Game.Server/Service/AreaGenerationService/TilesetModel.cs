using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// In-memory model of a tileset's .set data, sufficient for corner-matching tile resolution.
    /// Built offline by TilesetSetParser (unit tests, boot-time cache) or live via the NWNX Tileset plugin.
    /// </summary>
    public class TilesetModel
    {
        public string Resref { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsInterior { get; set; }
        public bool HasHeightTransition { get; set; }
        public float HeightTransition { get; set; }

        /// <summary>Terrain used for the area border ([GENERAL] Border).</summary>
        public string BorderTerrain { get; set; } = string.Empty;
        /// <summary>Terrain new areas are filled with ([GENERAL] Default) — the "solid" terrain for generation.</summary>
        public string DefaultTerrain { get; set; } = string.Empty;
        /// <summary>Primary walkable terrain ([GENERAL] Floor) — the "open" terrain for generation.</summary>
        public string FloorTerrain { get; set; } = string.Empty;

        public List<string> Terrains { get; set; } = new();
        public List<string> Crossers { get; set; } = new();

        /// <summary>Indexed by tile ID: Tiles[n] corresponds to [TILEn] in the .set file.</summary>
        public List<TileRecord> Tiles { get; set; } = new();
        public List<TileGroupRecord> Groups { get; set; } = new();
    }

    /// <summary>
    /// One [TILEn] entry. Corner and edge arrays use fixed orderings:
    /// corners clockwise from top-left [TL, TR, BR, BL]; edges clockwise from top [Top, Right, Bottom, Left].
    /// "Top" is the +Y (north) side of the unrotated tile model.
    /// </summary>
    public class TileRecord
    {
        public int TileId { get; set; }
        public string Model { get; set; } = string.Empty;
        public string WalkMesh { get; set; } = string.Empty;
        public string PathNode { get; set; } = string.Empty;
        public string ImageMap2D { get; set; } = string.Empty;

        /// <summary>Corner terrain names, [TL, TR, BR, BL].</summary>
        public string[] Corners { get; set; } = { "", "", "", "" };
        /// <summary>Corner height offsets, [TL, TR, BR, BL] (from TopLeftHeight etc.; 0 when absent).</summary>
        public int[] CornerHeights { get; set; } = { 0, 0, 0, 0 };
        /// <summary>Edge crosser names, [Top, Right, Bottom, Left]. Empty string = no crosser.</summary>
        public string[] Edges { get; set; } = { "", "", "", "" };

        /// <summary>Index of the [GROUPn] this tile belongs to, or -1. Group members are excluded from random terrain placement.</summary>
        public int GroupIndex { get; set; } = -1;

        public List<TileDoorRecord> Doors { get; set; } = new();

        // NWN tile orientation n = n * 90 degrees counterclockwise. With the clockwise-ordered
        // arrays above, the tile feature occupying world slot i is base[(i + orientation) % 4].
        // This convention is pinned empirically by tests that check hand-authored module areas
        // for corner consistency — if those fail, fix the formula here, nowhere else.
        public string GetCornerAt(int orientation, int cornerSlot)
        {
            return Corners[(cornerSlot + orientation) % 4];
        }

        public int GetCornerHeightAt(int orientation, int cornerSlot)
        {
            return CornerHeights[(cornerSlot + orientation) % 4];
        }

        public string GetEdgeAt(int orientation, int edgeSlot)
        {
            return Edges[(edgeSlot + orientation) % 4];
        }

        public bool HasAnyCrosser
        {
            get
            {
                foreach (var edge in Edges)
                {
                    if (!string.IsNullOrEmpty(edge))
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>Corner slot indices for TileRecord corner arrays.</summary>
    public static class CornerSlot
    {
        public const int TopLeft = 0;
        public const int TopRight = 1;
        public const int BottomRight = 2;
        public const int BottomLeft = 3;
    }

    /// <summary>Edge slot indices for TileRecord edge arrays.</summary>
    public static class EdgeSlot
    {
        public const int Top = 0;
        public const int Right = 1;
        public const int Bottom = 2;
        public const int Left = 3;
    }

    /// <summary>One [GROUPn] entry — a pre-designed multi-tile chunk.</summary>
    public class TileGroupRecord
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        /// <summary>Row-major tile IDs, Rows * Columns entries.</summary>
        public List<int> TileIds { get; set; } = new();
    }

    /// <summary>One [TILEnDOORm] entry — a door slot on a tile, positions relative to tile origin.</summary>
    public class TileDoorRecord
    {
        public int Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }
    }
}
