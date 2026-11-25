namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Calling a beast: the one-companion check and the spawn of a player's beast. Static callers reach the container's
    /// instance through <c>GameServices.BeastSpawn</c>.
    /// </summary>
    public interface IBeastSpawnService
    {
        /// <summary>
        /// Determines whether a player is free to spawn a new companion (beast or droid).
        /// The game engine only permits one henchman-type associate at a time, so beast and
        /// droid activation must share this check to prevent an orphaned, master-less companion.
        /// </summary>
        /// <param name="player">The player attempting to spawn a companion.</param>
        /// <returns>An error message if a companion is already active, otherwise an empty string.</returns>
        string GetCompanionSlotValidationError(uint player);

        /// <summary>
        /// Spawns a player's beast next to them and makes it their henchman.
        /// </summary>
        /// <param name="player">The beast's owner</param>
        /// <param name="beastId">The id of the beast entity</param>
        /// <param name="percentHeal">The percent of its hit points the beast is healed to after it spawns</param>
        void SpawnBeast(uint player, string beastId, int percentHeal);
    }
}
