#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Frontage
{
    /// <summary>
    /// One weighted structural building placeable the frontage system (BuildingFrontagePlanner) may
    /// erect along open-area perimeter edges and street margins to form canyon walls -- the
    /// hand-built promenade-family mechanism (skyscraper placeables standing on the margin, flush
    /// lines at ~10m pitch). DELIBERATELY separate from <see cref="DungeonDecorationEntry"/>:
    /// whole-building art is structure, not dressing (DecorationAnchoring.Excluded strips it from
    /// every scatter palette), and placement is geometric (footprint fit against the walkable grid)
    /// rather than palette-bucket driven. Footprints are measured model XY extents: FaceWidth spans
    /// along the fronted face, Depth extends
    /// into the non-walkable margin.
    /// </summary>
    public class BuildingFrontageEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        /// <summary>Model footprint extent (meters) along the fronted face.</summary>
        public float FaceWidth { get; set; } = 10f;
        /// <summary>Model footprint extent (meters) into the margin, perpendicular to the face.</summary>
        public float Depth { get; set; } = 10f;
        /// <summary>Hard per-area placement cap (0 = uncapped). Derived from the hand-built
        /// per-area counts: the dominant wall models stay uncapped, accents cap near their
        /// hand-built maxima so no single accent tower can blanket an area.</summary>
        public int MaxPerArea { get; set; }
        /// <summary>Minimum center distance (meters) between two placements of THIS model in one
        /// area (0 = no spacing rule), enforced omnidirectionally -- across parallel rows and
        /// facing pairs, not just within a collinear run. Mined for narrow repeat-risk accent
        /// towers: hand-built same-model NN medians run 18.9-36.0m for the swd2_elev002 lift
        /// cylinder; the salience audit extends the rule to every non-workhorse frontage
        /// model from the mined same-model cross-line nearest-neighbor distances. Workhorse
        /// models carry no spacing because hand-built areas stand them directly across the street
        /// from themselves (narpromena build007 mutually-facing pairs at 10.1m).</summary>
        public float MinSameModelSpacing { get; set; }

        /// <summary>Whether this model may be elected a street/run DOMINANT (the model a frontage
        /// run repeats). Mined workhorse classification over
        /// the hand-built fcx01 areas): a model is a workhorse when its two highest per-area
        /// counts sum to 20+ (hand-builders demonstrably wall whole streets with it: build007
        /// 36+30, build004 21+7, kyru08 10+10) AND its texture set is low-salience (diffuse
        /// neon-pixel share under 0.15 -- distinctive neon/emissive towers stay rare in
        /// hand-built areas regardless of size: the neon-clad build003 tops out at 1-4 per
        /// comparable-mass area). Non-eligible models still interleave as accents, but a street
        /// can never legally elect a distinctive tower and repeat it -- the clone-city
        /// report was a high-salience model elected dominant.</summary>
        public bool DominantEligible { get; set; }

        /// <summary>Visual family key (null/empty = the model stands alone). Models that share
        /// their dominant texture atlases read as the same building line to a player even when
        /// their meshes differ (the daf neon towers build001/002/003/005/006 share the
        /// daf_sw011_5/6 neon poster atlases; jsf_batimt02/04 share the full jsf_bldgtx set; the
        /// bitmap analysis found no same-mesh recolor pairs
        /// in the frontage pool). Per-area caps aggregate across the family via
        /// <see cref="FamilyMaxPerArea"/>.</summary>
        public string FamilyKey { get; set; }

        /// <summary>Hard per-area cap on the whole <see cref="FamilyKey"/> family (0 = none).
        /// Mined from hand-built per-area family totals in comparable-mass areas.</summary>
        public int FamilyMaxPerArea { get; set; }
    }
}
