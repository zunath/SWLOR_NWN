using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// What the Palette panel needs from the area it is placing into.
    /// </summary>
    /// <remarks>
    /// An interface rather than a direct reference to the area editor, so the palette stays a shell panel
    /// that knows nothing about area documents beyond "something can accept a placement right now".
    /// </remarks>
    public interface IAreaPlacementTarget
    {
        /// <summary>
        /// Arms placement for a blueprint. The next click in the map resolves it. Returns false when this
        /// area has no list for that type - loose items have none.
        /// </summary>
        bool ArmPlacement(ResourceType type, string resRef, PaletteSource source);

        /// <summary>
        /// The tileset this area is built from, or null when it cannot be resolved.
        /// </summary>
        /// <remarks>
        /// The Tiles palette has no content of its own - which tiles exist is a property of the open
        /// area, not of the module - so the panel has to ask whatever is in front what it is made of.
        /// </remarks>
        string? TilesetResRef { get; }

        /// <summary>
        /// Arms placement for a tile or tile group. The next click in the map stamps it into the area's
        /// tile grid. Returns false when the area's grid cannot be edited.
        /// </summary>
        bool ArmTilePlacement(TilePaletteEntry entry);
    }
}
