#nullable disable

using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Tileset
{
    public static class CornerSlot
    {
        public const int TopLeft = 0;
        public const int TopRight = 1;
        public const int BottomRight = 2;
        public const int BottomLeft = 3;
    }

    public static class EdgeSlot
    {
        public const int Top = 0;
        public const int Right = 1;
        public const int Bottom = 2;
        public const int Left = 3;
    }

    /// <summary>One door slot from a tileset tile record.</summary>
    public class TileDoorRecord
    {
        public int Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }
    }

    /// <summary>One pre-designed multi-tile group.</summary>
    public class TileGroupRecord
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<int> TileIds { get; set; } = new();
    }
}
