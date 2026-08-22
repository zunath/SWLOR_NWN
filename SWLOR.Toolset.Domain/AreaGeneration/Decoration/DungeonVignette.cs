#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// A small, evidence-backed multi-placeable grouping (e.g. crate stack, bench+lamp) placed as a
    /// single unit by <see cref="DungeonDecorationPlanner"/> rather than each member rolling
    /// independently — see <see cref="DungeonTilesetProfile.Vignettes"/>.
    /// </summary>
    public class DungeonVignette
    {
        public string Key { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public List<DungeonVignetteMember> Members { get; set; } = new();
    }
}
