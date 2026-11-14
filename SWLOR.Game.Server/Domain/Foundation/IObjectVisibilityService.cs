using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Per-player visibility of tagged objects. Static callers reach the container's instance through
    /// <c>GameServices.ObjectVisibility</c>; the event handlers are on <see cref="ObjectVisibilityService"/>.
    /// </summary>
    public interface IObjectVisibilityService
    {
        /// <summary>
        /// When the module loads, cycle through every area and every object to identify the visibility objects.
        /// </summary>
        void LoadVisibilityObjects();

        /// <summary>
        /// When a player enters the server, toggle visibility on all objects
        /// </summary>
        void LoadPlayerVisibilityObjects();

        /// <summary>
        /// Modifies the visibility of an object for a specific player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="target">The target object to adjust.</param>
        /// <param name="type">The new type of visibility to use.</param>
        void AdjustVisibility(uint player, uint target, VisibilityType type);

        /// <summary>
        /// Adjusts the visibility of a given object for a given player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="visibilityObjectId">The visibility object Id of the object to adjust.</param>
        /// <param name="type">The new visibility type to adjust to.</param>
        void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type);
    }
}
