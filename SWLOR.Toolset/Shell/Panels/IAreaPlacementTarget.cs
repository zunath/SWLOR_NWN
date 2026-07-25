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
        /// area has no list for that type - items and encounters have none.
        /// </summary>
        bool ArmPlacement(ResourceType type, string resRef);
    }
}
