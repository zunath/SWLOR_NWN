using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Shared.Domain.World.Contracts
{
    /// <summary>
    /// Service responsible for managing player-specific visibility state.
    /// </summary>
    public interface IPlayerVisibilityService
    {
        /// <summary>
        /// Loads visibility objects for a player when they enter the server.
        /// </summary>
        void LoadPlayerVisibilityObjects();

        /// <summary>
        /// Gets the visibility type for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <returns>The visibility type, or null if not set</returns>
        VisibilityType? GetPlayerObjectVisibility(string playerId, string visibilityObjectId);

        /// <summary>
        /// Sets the visibility type for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <param name="visibilityType">The visibility type to set</param>
        void SetPlayerObjectVisibility(string playerId, string visibilityObjectId, VisibilityType visibilityType);

        /// <summary>
        /// Removes visibility settings for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        void RemovePlayerObjectVisibility(string playerId, string visibilityObjectId);
    }
}
