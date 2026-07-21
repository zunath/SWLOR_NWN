using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Decoration
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
    }
}
