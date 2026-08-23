#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// A NAMED alternate decoration palette a tileset profile can declare alongside its standard one
    /// (see <see cref="DungeonTilesetProfile.DecorationProfiles"/>) -- e.g. fcx01's "ruined" profile,
    /// which carries the wreckage/rubble/debris/dirt-decal destruction content the STANDARD clean
    /// city palette deliberately excludes. A named profile fully REPLACES the standard
    /// Decorations/Vignettes lists when selected (no merging), so each profile reads as one coherent
    /// visual statement; the theme's own small accent list still layers on top as usual. Selected via
    /// <see cref="DungeonDetail.DecorationProfile"/> (theme declaration) or the Area Generator's
    /// explicit per-draft selection. Unknown or empty names fall back to the standard palette.
    /// </summary>
    public class DungeonDecorationProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();
        public List<DungeonVignette> Vignettes { get; set; } = new();

        /// <summary>
        /// True when this profile's clutter is genuinely organic junk (collapse debris, rubble
        /// drifts) whose pile members keep fully random rotations even under the tileset's urban
        /// placement grammar -- the one sanctioned exception to bearing alignment. The standard
        /// clean-city palette leaves this false so cargo reads as stacked/aligned goods.
        /// </summary>
        public bool OrganicClutterRotation { get; set; }
    }
}
