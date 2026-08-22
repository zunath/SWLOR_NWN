#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// STRUCTURAL anchoring requirement of a curated decoration entry -- what kind of architecture
    /// (if any) the blueprint's own art demands before a placement of it can read as intentional.
    /// Mined per resref from hand-built reference usage (placement count, distance-to-nearest
    /// building-architecture distribution, same-family nearest-neighbor spacing) plus the blueprint
    /// model's measured footprint -- see the July 2026 fcx01 semantic-context pass
    /// (_scratch_decor/mine_r7_semantics.py): a fence DOOR standing alone in an open plaza (the
    /// reported "gate without a wall") is the artifact class this classification exists to prevent.
    /// Orthogonal to <see cref="DecorationRole"/> (what arrangement mechanisms may place it) and
    /// <see cref="DecorationContext"/> (which bucket it is curated under): anchoring says what the
    /// ART physically needs.
    /// </summary>
    public enum DecorationAnchoring
    {
        /// <summary>Default: the art legitimately stands on its own (crates, planters, lamps,
        /// kiosks, rubble) subject only to its curated context bucket's rules.</summary>
        FreeStanding = 0,

        /// <summary>
        /// Must sit flush against a real architecture face -- within
        /// DungeonDecorationPlanner.FlushWallGap of a stamped structure footprint's cardinal face,
        /// bearing = that face's outward normal -- or not place at all. Mined from entries whose
        /// hand-built placements have median building-architecture distance ~0 (cargo stacked
        /// against tower walls: flush fraction 0.55-0.90 in the fcx01 reference areas). Never
        /// eligible for clutter piles, courtyard rings, doorway flanks, or plain wall runs, all of
        /// which would strand the item away from the face it needs.
        /// </summary>
        WallFlush = 1,

        /// <summary>
        /// Only meaningful as one segment of a composed multi-segment run (fence lines and their
        /// gate pieces). NO run-composition mechanism exists in DungeonDecorationPlanner -- the
        /// hand-built evidence shows these are butt-jointed chains at model-width pitch (fence
        /// family nearest-neighbor median 6.58m against a 7.12m segment model), a sub-tile
        /// continuous placement contract the per-tile stamping model cannot honor (and the fence
        /// door model measures 11.87m, wider than a whole 10m tile) -- so the planner strips
        /// RunSegment entries from every palette outright: absence is better than a free-standing
        /// gate. Composed fencing belongs to TILE vocabulary instead (see tds01's LayoutFenceCarver
        /// + FenceDoor01/02 CorridorInsert set pieces). The classification is kept so a future
        /// curated entry cannot silently leak back in as scatter.
        /// </summary>
        RunSegment = 2,

        /// <summary>
        /// Never placed by the generator under any mechanism: blueprints that are whole
        /// architecture fragments (swd_build007's model measures 10.92x10.92m -- an entire
        /// building), or that have no hand-built usage evidence in any curated family. Stripped
        /// from every palette at merge time as a hard guard.
        /// </summary>
        Excluded = 3
    }
}
