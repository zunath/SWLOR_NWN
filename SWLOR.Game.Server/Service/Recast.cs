using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The recast groups' names and visibility, and the cooldown deadlines of players and NPCs. The logic lives in
    /// <see cref="IRecastService"/>; the event handler stays here and every member forwards to the container's instance.
    /// </summary>
    public static class Recast
    {
        private static IRecastService Instance => FacadeAccessor.Resolve<IRecastService>();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheRecastGroups() => Instance.CacheRecastGroups();

        /// <summary>
        /// Retrieves the human-readable name of a recast group.
        /// </summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The name of a recast group.</returns>
        public static string GetRecastGroupName(RecastGroup recastGroup) => Instance.GetRecastGroupName(recastGroup);

        /// <summary>
        /// Retrieves the full human-readable name of a recast group.
        /// </summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The full name of a recast group.</returns>
        public static string GetRecastGroupDisplayName(RecastGroup recastGroup) => Instance.GetRecastGroupDisplayName(recastGroup);

        /// <summary>
        /// Returns true if a recast group should be shown on player-facing UI.
        /// </summary>
        /// <param name="recastGroup">The recast group to check.</param>
        /// <returns>true if the recast group is visible, false otherwise.</returns>
        public static bool IsRecastGroupVisible(RecastGroup recastGroup) => Instance.IsRecastGroupVisible(recastGroup);

        /// <summary>
        /// Returns true if a recast delay has not expired yet.
        /// Returns false if there is no recast delay or the time has already passed.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="recastGroup">The recast group to check</param>
        /// <returns>true if recast delay hasn't passed. false otherwise. If true, also returns a string containing a user-readable amount of time they need to wait. Otherwise it will be an empty string.</returns>
        public static (bool, string) IsOnRecastDelay(uint creature, RecastGroup recastGroup) =>
            Instance.IsOnRecastDelay(creature, recastGroup);

        /// <summary>
        /// Applies a recast delay on a specific recast group.
        /// If group is invalid or delay amount is less than or equal to zero, nothing will happen.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="group">The recast group to put this delay under.</param>
        /// <param name="delaySeconds">The number of seconds to delay.</param>
        public static void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds) =>
            Instance.ApplyRecastDelay(activator, group, delaySeconds);

        /// <summary>Publishes an existing cooldown deadline through the regular persistence and icon process.</summary>
        public static void ApplyRecastDelay(uint activator, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null) =>
            Instance.ApplyRecastDelay(activator, group, startedAt, endsAt, sourceTexture, iconTexture);

        public static void ReduceRecastDelay(uint activator, RecastGroup group, float reduceSeconds) =>
            Instance.ReduceRecastDelay(activator, group, reduceSeconds);
    }
}
