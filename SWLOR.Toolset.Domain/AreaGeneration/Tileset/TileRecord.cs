#nullable disable
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Tileset
{
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
}
