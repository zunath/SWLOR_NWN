#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// One planned decorative placeable spawn point: a resref, a flat (ungrounded, Z=0) world
    /// position, a facing, and the context it was planned under. Purely a data record; grounding
    /// and blueprint realization happen later in <see cref="Authoring.GeneratedAreaDocumentPopulator"/>,
    /// so planning remains unit-testable without a module workspace.
    /// </summary>
    public class PlannedDecoration
    {
        public string Resref { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public float Facing { get; set; }
        public DecorationContext Context { get; set; }

        /// <summary>Per-instance uniform visual scale (1 = none). Non-1 only for frontage
        /// buildings on families declaring <see cref="DungeonTilesetProfile.FrontageScaleJitter"/>;
        /// persisted as a .git VisualTransform struct by the generated-area document populator (the
        /// toolset and client both render it; hand-built SWLOR areas carry the same struct).</summary>
        public float VisualScale { get; set; } = 1f;

        /// <summary>
        /// Unscaled world-space radius of the placeable's declared XY footprint. Creature
        /// clearance multiplies this by <see cref="VisualScale"/> so scaled art reserves the same
        /// space it renders into.
        /// </summary>
        public float FootprintRadius { get; set; } = 1f;

        /// <summary>
        /// World-space SUPPORT ANCHOR for grounding, or null to ground at the placement's own XY
        /// (every ordinary decoration). Set by BuildingFrontagePlanner for frontage buildings: a
        /// point just inside the fronted open (platform) cell, so document realization samples the
        /// platform surface the building's face stands flush with -- never a chasm floor far below
        /// the footprint's center (fcx01's "holes" margins), which is what a naive center sample
        /// returns for a deep tower whose body overhangs the drop.
        /// </summary>
        public Vector2? GroundAnchor { get; set; }

        /// <summary>
        /// Plan-time absolute ground-height estimate (m) at <see cref="GroundAnchor"/>. The document
        /// populator independently interpolates the resolved tile's oriented corner-height profile
        /// at the anchor (or placement) XY before adding Position.Z, so sloped tiles are grounded
        /// correctly. 0 for anchor-less decorations and flat layouts.
        /// </summary>
        public float GroundZ { get; set; }
    }
}
