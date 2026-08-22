#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// Approximate physical size class of a decoration entry's ART, measured from the blueprint
    /// model's XY bounding box (decompiled .mdl verts -- see the round-8 size audit in
    /// _scratch_decor/r8_model_sizes.json): Small &lt; 1.2m, Medium 1.2-3m, Large 3-8m, Huge &gt;= 8m
    /// footprint. Drives size-aware repetition control under the urban grammar
    /// (DungeonDecorationPlanner): Huge entries place ONLY via composed industrial-yard rows;
    /// Large entries cap their per-row repeats so 6m containers never wall up back-to-back.
    /// Medium is the enum default (0) so every pre-existing palette entry keeps its behavior
    /// without a declaration.
    /// </summary>
    public enum DecorationSize
    {
        /// <summary>1.2-3m footprint (default -- ordinary fixtures/furniture).</summary>
        Medium = 0,
        /// <summary>Under 1.2m footprint (crates, trash cans, small lamps).</summary>
        Small = 1,
        /// <summary>3-8m footprint (shipping containers, vehicles, kiosks): per-row repeat caps
        /// and same-model spacing apply under the urban grammar.</summary>
        Large = 2,
        /// <summary>8m+ footprint (storage silos, industrial towers, parked starfighters):
        /// building-scale. Placed ONLY as composed industrial-yard rows/pairs with shared bearing,
        /// never by the generic run/pile/courtyard mechanisms, and never outside industrial-flavor
        /// zones.</summary>
        Huge = 3
    }
}
