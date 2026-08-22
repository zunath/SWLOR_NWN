#nullable disable
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Tileset
{
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
