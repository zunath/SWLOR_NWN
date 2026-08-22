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
    /// position, a facing, and the context it was planned under. Purely a data record — grounding
    /// (GetGroundHeight) and CreateObject happen in DungeonContentPlacer, which is the only part of
    /// this pass that touches the live engine, so Plan() itself is unit-testable without an area.
    /// </summary>
    public class PlannedDecoration
    {
        public string Resref { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public float Facing { get; set; }
        public DecorationContext Context { get; set; }

        /// <summary>Per-instance uniform visual scale (1 = none). Non-1 only for frontage
        /// buildings on families declaring <see cref="DungeonTilesetProfile.FrontageScaleJitter"/>;
        /// persisted as a .git VisualTransform struct offline (the toolset and client both render
        /// it -- hand-built SWLOR areas carry the same struct) and applied via
        /// SetObjectVisualTransform on the live path.</summary>
        public float VisualScale { get; set; } = 1f;

        /// <summary>
        /// World-space SUPPORT ANCHOR for grounding, or null to ground at the placement's own XY
        /// (every ordinary decoration). Set by BuildingFrontagePlanner for frontage buildings: a
        /// point just inside the fronted open (platform) cell, so the live path's
        /// GetGroundHeight sample lands on the platform surface the building's face stands flush
        /// with -- never on a chasm floor far below the footprint's center (fcx01's "holes"
        /// margins), which is what a naive center sample returns for a deep tower whose body
        /// overhangs the drop.
        /// </summary>
        public Vector2? GroundAnchor { get; set; }

        /// <summary>
        /// Plan-time absolute ground height (m) at <see cref="GroundAnchor"/> -- the anchor tile's
        /// Height index times the tileset's height transition (see
        /// ResolvedLayout.HeightTransition). 0 for anchor-less decorations and for flat layouts.
        /// The offline review module emits Z = GroundZ + Position.Z (Position.Z stays the height
        /// OFFSET above ground), and the live self-test asserts GetGroundHeight at the anchor
        /// agrees with this value, keeping the two paths from diverging.
        /// </summary>
        public float GroundZ { get; set; }
    }
}
