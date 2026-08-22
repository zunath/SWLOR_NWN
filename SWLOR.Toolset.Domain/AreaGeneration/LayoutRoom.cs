#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    public class LayoutRoom
    {
        public int Id { get; set; }
        public RoomRole Role { get; set; }
        /// <summary>Tile coordinates of the room's representative center, used for spawn/objective placement and path validation.</summary>
        public (int X, int Y) CenterTile { get; set; }
        /// <summary>All tile coordinates belonging to this room's open space.</summary>
        public List<(int X, int Y)> Tiles { get; set; } = new();
        /// <summary>
        /// True for a WallRoom set piece registered by LayoutGroupStamper: a pre-designed multi-tile
        /// chunk whose interior is walkable via its own baked model walkmesh, not the abstract
        /// corner-terrain path graph (its Tiles are fully-solid corner cells and its pathnodes are
        /// often not 'A'). Content placement and path validation must skip these rooms.
        /// </summary>
        public bool IsSetPiece { get; set; }

        /// <summary>
        /// The terrain label this room's interior is carved from. Defaults to the layout's primary
        /// OpenTerrain; districted RoomsAndCorridors/Tunnel layouts may carve a room from
        /// MacroLayoutParameters.SecondaryOpenTerrain instead (see MacroLayoutParameters.SecondaryOpenTerrain).
        /// Always populated by every layout style's room-building path, so downstream consumers
        /// (LayoutGroupStamper's OpenSetPiece matching) can rely on it rather than assuming the
        /// layout's single OpenTerrain applies to every room.
        /// </summary>
        public string OpenTerrain { get; set; } = string.Empty;
    }
}
