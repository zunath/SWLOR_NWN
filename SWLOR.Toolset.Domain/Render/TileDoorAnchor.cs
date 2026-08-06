using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// One place in an area where a door may be hung: a door node a placed tile declares, resolved
    /// into world space.
    /// </summary>
    /// <remarks>
    /// A door is not free-standing scenery. The tile owns the doorway - the frame, the gap in the
    /// wall, the walkmesh cut - and the door leaf only makes sense filling it, which is why Aurora
    /// will not let one be dropped anywhere else. The nodes come from the tileset's
    /// <c>[TILEnDOORd]</c> blocks (see <see cref="GameData.Tilesets.TileDoorDefinition"/>), whose
    /// coordinates are tile-local and centred, so each one is carried into the world by the same
    /// transform that placed its tile - rotation included.
    /// </remarks>
    public sealed record TileDoorAnchor
    {
        /// <summary>Index into the area's Tile_List of the tile that declares this node.</summary>
        public required int TileIndex { get; init; }

        /// <summary>Which of that tile's door nodes this is, in .set order.</summary>
        public required int DoorIndex { get; init; }

        /// <summary>The tileset's door Type for this node - the door's shape class, not a doortypes.2da row.</summary>
        public required int Type { get; init; }

        /// <summary>World position of the doorway, on the tile's own floor height.</summary>
        public required Vector3 Position { get; init; }

        /// <summary>
        /// The heading the door hangs at, as the same (x, y) unit vector an instance's orientation
        /// uses - the tile's own door orientation, turned by however far the tile itself was turned.
        /// </summary>
        public required Vector2 Orientation { get; init; }
    }
}
