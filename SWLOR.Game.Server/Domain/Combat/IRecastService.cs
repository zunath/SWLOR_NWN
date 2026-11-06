using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The recast groups' names and visibility, and the cooldown deadlines of players and NPCs.</summary>
    public interface IRecastService
    {
        /// <summary>Reads the recast group enumeration into the name caches (module cache handler).</summary>
        void CacheRecastGroups();

        /// <summary>Retrieves the human-readable name of a recast group.</summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The name of a recast group.</returns>
        string GetRecastGroupName(RecastGroup recastGroup);

        /// <summary>Retrieves the full human-readable name of a recast group.</summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The full name of a recast group.</returns>
        string GetRecastGroupDisplayName(RecastGroup recastGroup);

        /// <summary>Returns true if a recast group should be shown on player-facing UI.</summary>
        /// <param name="recastGroup">The recast group to check.</param>
        /// <returns>true if the recast group is visible, false otherwise.</returns>
        bool IsRecastGroupVisible(RecastGroup recastGroup);

        /// <summary>
        /// Returns true if a recast delay has not expired yet.
        /// Returns false if there is no recast delay or the time has already passed.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="recastGroup">The recast group to check</param>
        /// <returns>true if recast delay hasn't passed. false otherwise. If true, also returns a string containing a user-readable amount of time they need to wait. Otherwise it will be an empty string.</returns>
        (bool, string) IsOnRecastDelay(uint creature, RecastGroup recastGroup);

        /// <summary>
        /// Applies a recast delay on a specific recast group.
        /// If group is invalid or delay amount is less than or equal to zero, nothing will happen.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="group">The recast group to put this delay under.</param>
        /// <param name="delaySeconds">The number of seconds to delay.</param>
        void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds);

        /// <summary>Publishes an existing cooldown deadline through the regular persistence and icon process.</summary>
        void ApplyRecastDelay(uint activator, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null);

        /// <summary>Shortens a recast delay by a number of seconds, removing it when it has run out.</summary>
        void ReduceRecastDelay(uint activator, RecastGroup group, float reduceSeconds);
    }
}
