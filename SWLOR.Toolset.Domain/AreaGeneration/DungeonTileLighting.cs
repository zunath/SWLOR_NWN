#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Per-tile lighting written into every generated tile of a theme. Values are tile light
    /// color indices (see TILE_MAIN_LIGHT_COLOR_* / TILE_SOURCE_LIGHT_COLOR_*); defaults match
    /// the hand-built tdt01 cave areas.
    /// </summary>
    public class DungeonTileLighting
    {
        public int MainLight1 { get; set; }
        public int MainLight2 { get; set; }
        public int SourceLight1 { get; set; } = 8;
        public int SourceLight2 { get; set; } = 8;
    }
}
