using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Tileset
{
    /// <summary>One [GROUPn] entry — a pre-designed multi-tile chunk.</summary>
    public class TileGroupRecord
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        /// <summary>Row-major tile IDs, Rows * Columns entries.</summary>
        public List<int> TileIds { get; set; } = new();
    }
}
