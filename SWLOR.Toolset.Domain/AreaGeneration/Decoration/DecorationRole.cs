#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// Semantic role of a curated decoration entry, driving which ARRANGEMENT mechanisms may place
    /// it (see DungeonDecorationPlanner). Independent of <see cref="DecorationContext"/> (where a
    /// single placement may anchor): the role says what KIND of thing the art is, mined from how
    /// hand-built reference areas actually arrange that resref.
    /// </summary>
    public enum DecorationRole
    {
        /// <summary>Default: an ordinary fixture placed by the run/centerpiece/flank/courtyard
        /// mechanisms exactly as before roles existed.</summary>
        Fixture = 0,
        /// <summary>
        /// Pile-able junk (crates, containers, barrels, rubble, trash): eligible for the clutter-pile
        /// arrangement (see DungeonDecorationPlanner.PlanClutterPile) IN ADDITION to whatever context
        /// bucket the entry is curated under. A palette with no Clutter entries gets no pile pass at
        /// all (and an unchanged budget split / RNG stream).
        /// </summary>
        Clutter = 1,
        /// <summary>
        /// A flat ground decal (dirt patch, stain, floor marking). NEVER placed stand-alone by any
        /// run/centerpiece/flank mechanism -- only layered under a clutter pile, or as a courtyard
        /// center that receives clutter on top. Hand-built evidence: decals appear as layering under
        /// junk arrangements, not as lone patches in open plazas.
        /// </summary>
        GroundDecal = 2,
        /// <summary>
        /// A large narrative one-off (parked/crashed vehicle, monument, altar). Must read as
        /// anchored to something: only placed via StructureAdjacent/CorridorSide (road-side)
        /// buckets, doorway flanks, or as a curated vignette member -- the planner strips Landmark
        /// entries out of the RoomCenter and WallAdjacent buckets so one can never float alone in
        /// the middle of an open plaza.
        /// </summary>
        Landmark = 3
    }
}
