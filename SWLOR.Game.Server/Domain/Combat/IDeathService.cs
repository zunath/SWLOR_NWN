using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Player death, respawn, the XP debt penalty and where a player respawns.</summary>
    public interface IDeathService
    {
        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        void OnPlayerDying();

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        void OnPlayerDeath();

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        void OnPlayerRespawn();

        /// <summary>
        /// Handles setting a player's respawn point if they don't have one set already.
        /// </summary>
        void InitializeRespawnPoint();

        /// <summary>
        /// Returns where a player respawns: their registered medical center, or the default respawn
        /// point (DTH_DEFAULT_RESPAWN_POINT) when no medical center is saved, its area no longer exists,
        /// or the saved point is not on walkable ground. Never returns the place the player died.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>The location to respawn at.</returns>
        Location GetRespawnLocation(Player dbPlayer);

        /// <summary>
        /// Whether a player record names a respawn area at all.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>true if a respawn area resref is saved, false otherwise.</returns>
        bool HasRespawnArea(Player dbPlayer);
    }
}
