#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// A single weighted decorative placeable choice for one <see cref="DecorationContext"/> bucket
    /// within a curated palette. See <see cref="DungeonDetail.Decorations"/> (theme accents) and
    /// <see cref="DungeonTilesetProfile.Decorations"/> (the tileset family's own bulk palette).
    /// </summary>
    public class DungeonDecorationEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public DecorationContext Context { get; set; } = DecorationContext.WallAdjacent;
        /// <summary>Semantic role driving arrangement eligibility -- see <see cref="DecorationRole"/>.</summary>
        public DecorationRole Role { get; set; } = DecorationRole.Fixture;

        /// <summary>Structural anchoring requirement of the blueprint's art -- see
        /// <see cref="DecorationAnchoring"/>.</summary>
        public DecorationAnchoring Anchoring { get; set; } = DecorationAnchoring.FreeStanding;

        /// <summary>
        /// True only for street-furniture entries that legitimately stand ON a carved road lane
        /// (streetlight/lamp-family fixtures -- hand-built fcx01 streets carry their lamps and light
        /// strips on the road surface itself). Under a tileset's urban placement grammar (see
        /// <see cref="DungeonTilesetProfile.UrbanDressing"/>) every OTHER placement keeps the road
        /// ribbon clear: a tile whose own edges carry the road crosser only ever hosts entries with
        /// this flag; everything else sets back to the adjacent road-margin tiles, facing the street.
        /// Inert (never read) for tilesets without the urban grammar.
        /// </summary>
        public bool AllowOnRoadSurface { get; set; }

        /// <summary>
        /// Physical size class of the blueprint's art -- see <see cref="DecorationSize"/>. Used
        /// for conservative generated-creature clearance in every composition and for size-aware
        /// repetition control under the urban placement grammar.
        /// </summary>
        public DecorationSize Size { get; set; } = DecorationSize.Medium;

        /// <summary>
        /// Optional measured XY footprint radius in meters. Zero uses the conservative radius for
        /// <see cref="Size"/>; set this when a model's measured footprint is available.
        /// </summary>
        public float FootprintRadius { get; set; }

        /// <summary>
        /// District affinity of this entry (see <see cref="DistrictFlavor"/>), evidence-derived
        /// from which hand-built area TYPE uses the resref: an EMPTY map means the entry serves
        /// every district at its base <see cref="Weight"/> (and is the only state non-urban
        /// tilesets ever use); a non-empty map means the entry's effective weight in a room of
        /// flavor F is DistrictWeights[F], and the entry is EXCLUDED from rooms whose flavor is
        /// absent from the map. Inert for tilesets without the urban grammar.
        /// </summary>
        public Dictionary<DistrictFlavor, int> DistrictWeights { get; set; } = new();

        /// <summary>
        /// Hard per-area placement cap for this resref across every arrangement mechanism
        /// (0 = uncapped), derived from the hand-built per-area p95 within the entry's district --
        /// the size-aware repetition-control backstop that keeps one fixture from blanketing a
        /// whole generated area. Only enforced under the urban grammar.
        /// </summary>
        public int MaxPerArea { get; set; }

        /// <summary>
        /// Z step (meters) at which one stacked copy of this entry sits directly above a base copy
        /// -- 0 (the default) means the art never stacks. Mined from the hand-built stacked-cargo
        /// evidence: the dressed city areas' ELEVATED dressing is dominated by cargo stacked one
        /// model-height above its own base copy (nsshipyard 125 swd_conta003 at Z 0.96,
        /// narscorpd 29 _mdrn_pl_crate08 at Z ~1.46, family-wide stack rates 0.2-0.55), NOT by
        /// signage -- lit sign-family elevated counts are only 8-36 per hand-built area. Read only
        /// under the urban grammar by the pile/depot mechanisms (see
        /// DungeonDecorationPlanner.TryStackCargo); entries without a declaration draw no stacking
        /// RNG at all, so non-urban plans stay byte-identical.
        /// </summary>
        public float StackHeight { get; set; }
    }
}
