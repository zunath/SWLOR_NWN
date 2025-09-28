using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Shared.Domain.World.Contracts
{
    /// <summary>
    /// Service responsible for core visibility management operations.
    /// </summary>
    public interface IObjectVisibilityService
    {
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
