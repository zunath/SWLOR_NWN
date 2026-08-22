#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A named layout shape: style plus tuning knobs, independent of any tileset. The template's
    /// AccentTerrain stays empty — a nonzero AccentDensity expresses intent, and the actual
    /// terrain name comes from the tileset profile at composition time.
    /// </summary>
    public class DungeonLayoutProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public MacroLayoutParameters Template { get; set; } = new();
    }
}
