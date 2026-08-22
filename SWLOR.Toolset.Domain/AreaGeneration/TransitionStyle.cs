#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// How a transition is realized in the finished area. Door substitution is opportunistic and
    /// tileset-dependent (see TileDoorPlanner) — every transition starts out and may remain Placeable.
    /// </summary>
    public enum TransitionStyle
    {
        /// <summary>Realized as a placeable spawned on <see cref="TransitionPoint.Tile"/> (original behavior).</summary>
        Placeable = 0,
        /// <summary>Realized as a real tileset door embedded in the room's wall.</summary>
        Door = 1,
        /// <summary>
        /// Realized as a themed 1x1 tileset "exit" group tile (e.g. tdt01 Exit01-03) pinned into the
        /// room's wall, with a real door spawned in its door slot (see GroupExitPlanner). Exit-kind
        /// transitions only; reuses the same Door*/DoorCell world-transform fields as Door style.
        /// </summary>
        GroupExit = 2
    }
}
