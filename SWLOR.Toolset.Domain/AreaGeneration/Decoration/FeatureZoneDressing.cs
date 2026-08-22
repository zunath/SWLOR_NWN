#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// How a 1x1 area-marking FEATURE TILE (see <see cref="DungeonTilesetProfile.FeatureTiles"/>)
    /// obligates set dressing when it lands inside an open room under a tileset's urban placement
    /// grammar -- the "empty zone decal" rule: a large area-marking tile IMPLIES content (a grass
    /// patch implies a park; a fountain court implies seating), so placing one obligates a composed
    /// ensemble instead of a bare marker (the reported "a park with no park" artifact -- a bare
    /// green lawn tile standing in a civic plaza). Declared per feature group name via
    /// DungeonTilesetProfileBuilder.FeatureTile; consumed by
    /// DungeonDecorationPlanner.PlanZoneDressings. Feature tiles whose own art already fills the
    /// cell (trees, water pools, treasure mounds, pillars) declare nothing and stay untouched.
    /// </summary>
    public enum FeatureZoneDressing
    {
        /// <summary>No dressing obligation (default; every pre-existing feature tile).</summary>
        None = 0,
        /// <summary>
        /// A flat, empty area marker (grass lawn, bare court): obligates a FULL ensemble ON the
        /// tile -- a centerpiece (tree/monument) plus a facing satellite ring (benches, lights).
        /// </summary>
        Lawn = 1,
        /// <summary>
        /// The tile's own art occupies the cell center (a fountain): obligates only a facing
        /// satellite surround at the tile margin -- no centerpiece item.
        /// </summary>
        Centerpiece = 2
    }
}
