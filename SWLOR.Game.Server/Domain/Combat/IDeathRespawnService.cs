using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Where a player respawns and what dying costs: the respawn point, the respawn location, the XP debt penalty and the audit lines.</summary>
    public interface IDeathRespawnService
    {
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

        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        void SendToHomePoint(uint player);

        /// <summary>
        /// Applies death penalties for a player: the XP debt, less the medical center and Social reductions, or none when a
        /// death-debt waiver was claimed.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        /// <returns>The player's total XP debt after the penalty.</returns>
        int ApplyPenalties(uint player);

        /// <summary>
        /// Writes the audit entry with details of a death (name, area and killer) to the Death audit group.
        /// </summary>
        /// <param name="player">The player who died</param>
        void WriteAudit(uint player);

        /// <summary>
        /// Writes the audit entry of a respawn to the Death audit group.
        /// </summary>
        /// <param name="player">The player who respawned</param>
        /// <param name="xpDebt">The player's total XP debt after the death's penalty, which a waived death leaves unchanged</param>
        void WriteAudit(uint player, int xpDebt);
    }
}
