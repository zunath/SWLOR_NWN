using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The recast groups' names and visibility, and the cooldown deadlines of players and NPCs.</summary>
    public interface IRecastService : IRecastDelays
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

        /// <summary>Publishes an existing cooldown deadline through the regular persistence and icon process.</summary>
        void ApplyRecastDelay(uint activator, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null);

        /// <summary>Shortens a recast delay by a number of seconds, removing it when it has run out.</summary>
        void ReduceRecastDelay(uint activator, RecastGroup group, float reduceSeconds);
    }
}
