using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Contracts
{
    public interface IRecastService
    {
        /// <summary>
        /// Retrieves the human-readable name of a recast group.
        /// </summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The name of a recast group.</returns>
        string GetRecastGroupName(RecastGroup recastGroup);

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
        /// <param name="ignoreRecastReduction">If true, recast reduction bonuses are ignored.</param>
        void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds, bool ignoreRecastReduction);
    }
}
