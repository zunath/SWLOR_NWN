#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// One weighted street-dressing placeable the STREET pass (DungeonDecorationPlanner.
    /// PlanStreetDressing) may lay along carved road-lane cells -- the hand-built dressed-street
    /// fill mechanism. Evidence (per-road-tile inventory over the dressed promenade-family fcx01
    /// areas, July 2026 street-dressing pass): hand-built ROAD tiles are dressed DENSER than plain
    /// plaza tiles (2.1-6.5 decoratives per road tile vs 2.1-4.4 per plain tile) via three layers --
    /// the municipal lamp line (already composed by the road lamp-line mechanism), flat road-marking
    /// plates near one per road tile (narpromena 23/26, nsshipyard 44/38, narscorpd 37/35), and
    /// margin accents (trash cans/barriers/consoles/holo signs) at ~0.5-1 per road tile. Room-anchored
    /// interior mechanisms cannot reach this surface on corridor-heavy layouts (Complex rooms cover
    /// ~14-17% of a 32x32 grid), which measured 2.1-2.6 decoratives per open tile against the dressed
    /// hand-built band's 2.85 floor -- this list is the street-anchored pool that closes that gap.
    /// DELIBERATELY separate from <see cref="DungeonDecorationEntry"/>: placement is lane-geometry
    /// driven (road-edge cells, lane axis, margin side), not room/palette-bucket driven, and the road
    /// ribbon's integrity contract (only declared street-legal art may stand on it) stays a single
    /// explicit list.
    /// </summary>
    public class StreetDressingEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public StreetDressingKind Kind { get; set; } = StreetDressingKind.RoadMarking;
        /// <summary>Hard per-area placement cap for this resref (0 = uncapped), counted against the
        /// SHARED per-area usage ledger, so a resref that also appears in the scatter palette (e.g.
        /// swd_trash01's clutter curation) keeps one combined hand-built-derived ceiling.</summary>
        public int MaxPerArea { get; set; }
    }
}
