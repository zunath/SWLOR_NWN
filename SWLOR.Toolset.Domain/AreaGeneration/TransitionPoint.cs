#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A point where the area connects to the outside world. Assigned by the shared layout
    /// post-pass to fully-open tiles in distinct rooms, spread apart by geodesic distance.
    /// The first Entrance is the primary arrival anchor.
    /// </summary>
    public class TransitionPoint
    {
        public TransitionKind Kind { get; set; }
        /// <summary>
        /// Tile the transition sits on/arrives at — always a fully open room cell. For Door and
        /// GroupExit styles this is the room-side walkable cell adjacent to the doorway
        /// (<see cref="DoorwayCell"/>), relocated from the original assignment by the planners;
        /// for Placeable style it is unchanged from the layout post-pass.
        /// </summary>
        public (int X, int Y) Tile { get; set; }
        /// <summary>Id of the LayoutRoom hosting this transition.</summary>
        public int RoomId { get; set; }

        /// <summary>
        /// How this transition is realized. Placeable unless TileDoorPlanner or GroupExitPlanner
        /// substitutes a door.
        /// </summary>
        public TransitionStyle Style { get; set; } = TransitionStyle.Placeable;
        /// <summary>
        /// Door style: the solid-side terminator cell now hosting the doorway wall tile. GroupExit
        /// style: the cell now pinned with the exit group's tile (no separate terminator — the group
        /// tile carries no crosser edges).
        /// </summary>
        public (int X, int Y) DoorCell { get; set; }
        /// <summary>
        /// Door/GroupExit styles: the wall cell whose tile was substituted to host the doorway or
        /// exit set piece. For Door style this is the room-edge doorway tile (distinct from both
        /// <see cref="Tile"/>, the open room-side anchor, and <see cref="DoorCell"/>, the solid
        /// terminator); for GroupExit it equals <see cref="DoorCell"/>.
        /// </summary>
        public (int X, int Y) DoorwayCell { get; set; }
        /// <summary>Door/GroupExit style only: world-space X of the door object.</summary>
        public float DoorX { get; set; }
        /// <summary>Door/GroupExit style only: world-space Y of the door object.</summary>
        public float DoorY { get; set; }
        /// <summary>Door/GroupExit style only: world-space Z of the door object.</summary>
        public float DoorZ { get; set; }
        /// <summary>Door/GroupExit style only: world-space facing (degrees, normalized to (-180, 180]) of the door object.</summary>
        public float DoorOrientation { get; set; }
    }
}
