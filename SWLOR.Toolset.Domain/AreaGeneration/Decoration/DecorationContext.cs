#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// Placement context a curated decoration entry is eligible for, chosen from hand-built reference
    /// areas: most decorative placeables hug a room's perimeter (streetlights, planters, crates, wall
    /// clutter), a minority sit as room centerpieces, long/narrow "corridor-like" rooms get a lighter
    /// lining, and small clutter clusters near doorways.
    /// </summary>
    public enum DecorationContext
    {
        /// <summary>Hugs a room's perimeter, offset toward the wall and facing back into the room.</summary>
        WallAdjacent = 0,
        /// <summary>A rare centerpiece placed near (never on) a large room's CenterTile.</summary>
        RoomCenter = 1,
        /// <summary>Lines the perimeter of a long/narrow "corridor-like" room.</summary>
        CorridorSide = 2,
        /// <summary>Small clutter near a transition's doorway.</summary>
        DoorwayFlank = 3,
        /// <summary>
        /// The central anchor of a composed courtyard arrangement (see
        /// DungeonDecorationPlanner.PlanCourtyard): one item standing at the interior anchor tile of a
        /// large plaza-like room, ringed by <see cref="Courtyard"/> items. Mined from hand-built fcx01
        /// interior arrangements (items &gt;2 tiles from walls/roads cluster as a centerpiece -- floor
        /// decal, floor light, small structure -- with a 4-13-item ring at radius ~4-9m around it).
        /// Only placed as part of a courtyard; never used as generic scatter.
        /// </summary>
        CourtyardCenter = 4,
        /// <summary>
        /// A ring/surround member of a composed courtyard arrangement (light poles, containers,
        /// planters, kiosks standing around a <see cref="CourtyardCenter"/> item) -- see
        /// DungeonDecorationPlanner.PlanCourtyard. Only placed as part of a courtyard.
        /// </summary>
        Courtyard = 5,
        /// <summary>
        /// Stands within one tile of a stamped multi-tile structure (an OpenSetPiece building
        /// footprint pinned by LayoutGroupStamper): sign panels, holo billboards, barriers that read
        /// as attached to a building's frontage rather than free-standing street furniture. Falls
        /// back to nothing (NOT WallAdjacent) when a layout has no stamped structures, so entries
        /// curated here never free-stand in the open -- the reported "sign panel next to a knee-high
        /// divider" artifact.
        /// </summary>
        StructureAdjacent = 6,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a member of a composed
        /// clutter pile (see DungeonDecorationPlanner.PlanClutterPile) -- 3-8 junk items packed
        /// within a ~1.2-2.5m radius, drawn from the room's own junk motif over the palette's
        /// <see cref="DecorationRole.Clutter"/> entries. This is the dominant hand-built arrangement
        /// (75% of hand-built fcx01 decoratives sit within 3m of another decorative; all-NN median
        /// 1.6m) that independent per-tile scatter cannot produce.
        /// </summary>
        ClutterPile = 7,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a ground decal (dirt patch,
        /// floor stain, floor marking) layered UNDER a clutter pile -- see
        /// DungeonDecorationPlanner.PlanClutterPile. Decals never stand alone: hand-built areas use
        /// them exclusively as layering beneath junk arrangements, so the planner only ever emits a
        /// <see cref="DecorationRole.GroundDecal"/> entry through a pile (or as a courtyard center
        /// that gets clutter layered on top).
        /// </summary>
        GroundDecal = 8,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one member of a composed
        /// industrial cargo-yard row (see DungeonDecorationPlanner.PlanCargoYard) -- 2-3 copies of
        /// one <see cref="DecorationSize.Huge"/> building-scale model standing on consecutive wall
        /// tiles at shared bearing, in an <see cref="DistrictFlavor.Industrial"/>-flavor room. The
        /// ONLY mechanism that may place Huge art.
        /// </summary>
        CargoYard = 9,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): the central item of a
        /// composed mid-room ensemble (see DungeonDecorationPlanner.PlanInteriorEnsemble /
        /// PlanZoneDressings) -- a civic monument garden's monument, a commercial plaza island's
        /// kiosk, a park lawn's tree. Always committed together with at least the ensemble's
        /// minimum satellite count, never free-standing.
        /// </summary>
        EnsembleCenter = 10,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a satellite member of a
        /// composed mid-room ensemble (benches/planters/lamps facing the
        /// <see cref="EnsembleCenter"/> item, or the facing surround of a dressed feature tile).
        /// </summary>
        EnsembleMember = 11,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one crate/cargo unit of a
        /// composed industrial DEPOT block (see DungeonDecorationPlanner.PlanDepotBlock) -- dense
        /// butt-jointed rows at near-model-width pitch with a shared bearing, mixed crate heights,
        /// and end-of-row satellite props. The hand-built shipyard/dock storage pattern
        /// (crate-family nearest-neighbor median under 1m, 93% within 2.2m, colinear runs of 4-12,
        /// cluster bearing dominant-share 0.81 in the interior-placement audit).
        /// </summary>
        DepotRow = 12,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one structural building
        /// placeable of a composed street-frontage line (see BuildingFrontagePlanner) -- a
        /// skyscraper/tower model standing on the non-walkable margin flush against an open cell's
        /// boundary, bearing = the face's outward normal, at ~10m pitch along the run. The
        /// hand-built promenade-family canyon mechanism: pw_ar_narpromena (12x12) walls its plaza
        /// with 30 swd_build* placeables on flat cobble (zero building tiles), build007 rows at
        /// 9.8-10.1m center pitch, 100% cardinal bearings in the frontage audit.
        /// Deliberately a separate channel from decoration clutter: DecorationAnchoring.Excluded
        /// still strips whole-building art from every scatter palette.
        /// </summary>
        BuildingFrontage = 13,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a wall-mounted sign/holo
        /// panel placed on a building FACE (a stamped structure tile's face or a
        /// <see cref="BuildingFrontage"/> placeable's face) at an evidence-derived height band,
        /// slightly proud of the face, bearing = the face's outward normal -- see
        /// BuildingFrontagePlanner.PlanFacadeMounts. Hand-built dense city areas carry 0.13-0.23
        /// of their decoratives above Z 0.5m, dominated by holo signage attached to building
        /// faces (sign-family median face distance ~0, Z medians 2.4-6.6m).
        /// </summary>
        FacadeMount = 14,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a flat road-marking floor
        /// plate laid ON a carved street lane cell (see DungeonDecorationPlanner.PlanStreetDressing
        /// and <see cref="StreetDressingEntry"/>). Hand-built promenade streets pave their lanes
        /// with marking plates at near-one-per-road-tile rates (pw_ar_narpromena 23 swd_florrd01
        /// on 26 road tiles, pw_ar_nsshipyard 44/38, pw_ar_narscorpd 37/35), 100% cardinal-aligned
        /// with the lane. Flat paint, not an obstruction -- the road ribbon stays a clear walkway.
        /// </summary>
        RoadMarking = 15,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a small street-furniture
        /// accent (trash can, barrier, console, holo sign) standing at the EDGE of a street lane
        /// cell, offset toward the road margin and facing back into the lane -- the hand-built
        /// street-stretch fill pattern (narpromena 22 swd_trash01 on its 26 road tiles,
        /// ns_comrcial_ka 40 _mdrn_pl_barrimw on 63, narshadaar_promi trash/barrier/console/holo
        /// rows at ~1 per road tile). See DungeonDecorationPlanner.PlanStreetDressing.
        /// </summary>
        StreetAccent = 16
    }
}
