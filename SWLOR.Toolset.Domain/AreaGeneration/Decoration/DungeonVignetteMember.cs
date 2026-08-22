#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// One placeable within a <see cref="DungeonVignette"/>: a resref plus its offset (in world units,
    /// pre-rotation) from the vignette's anchor tile and an additional facing offset (degrees) applied
    /// on top of the anchor's own "face into the room" facing. Mined from hand-built co-occurrence
    /// evidence (nearest-neighbor placeable pairs/triples within ~3-5m — see decoration_evidence/
    /// mine_evidence.py's pairwise clustering pass) — e.g. a bench+lamp or table+chairs grouping.
    /// </summary>
    public class DungeonVignetteMember
    {
        public string Resref { get; set; } = string.Empty;
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float FacingOffset { get; set; }
    }
}
