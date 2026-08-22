#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// The overall shape a macro layout carves. Styles are modeled on hand-built SWLOR areas:
    /// organic caverns (Korriban caverns), dense corridor warrens (Veles sewers), and
    /// wall-sharing packed rooms (facility interiors).
    /// </summary>
    public enum DungeonLayoutStyle
    {
        /// <summary>Rectangular rooms joined by corridors, with optional loop connections.</summary>
        RoomsAndCorridors = 0,
        /// <summary>Cellular-automata caves: winding, blobby open space with nooks and pockets.</summary>
        OrganicCave = 1,
        /// <summary>Maze-like corridor network with small chambers and loops (sewer/undercity feel).</summary>
        Warren = 2,
        /// <summary>Space subdivided into rooms sharing walls, joined by door gaps (facility feel).</summary>
        PackedRooms = 3,
        /// <summary>Near-perfect maze of long winding 1-corridor-wide passages with a few small chambers at junctions.</summary>
        Labyrinth = 4
    }
}
