#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// How one <see cref="StreetDressingEntry"/> stands on a carved street lane cell -- see
    /// DungeonDecorationPlanner.PlanStreetDressing.
    /// </summary>
    public enum StreetDressingKind
    {
        /// <summary>A flat road-marking floor plate laid on the lane surface itself, cardinal-aligned
        /// with the lane axis (walkable paint, never an obstruction).</summary>
        RoadMarking = 0,
        /// <summary>A small street-furniture accent (trash can, barrier, console, holo sign)
        /// standing at the lane cell's margin edge, facing back into the street.</summary>
        MarginAccent = 1
    }
}
