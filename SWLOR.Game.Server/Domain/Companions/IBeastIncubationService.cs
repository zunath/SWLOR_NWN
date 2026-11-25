using System;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Beast incubation: the incubator terminal, the beast egg a finished job makes, the tester weekday overrides, DNA
    /// extraction objects and the removal of a property's jobs. Static callers reach the container's instance through
    /// <c>GameServices.BeastIncubation</c>; the handlers are on <see cref="BeastIncubationService"/>.
    /// </summary>
    public interface IBeastIncubationService
    {
        /// <summary>
        /// When a player uses an incubator terminal, the incubator window opens, unless the player lacks the perk or
        /// another player's job is running there.
        /// </summary>
        void UseIncubator();

        /// <summary>
        /// Finishes an incubation job: rolls for a mutation, creates the beast egg with the job's purities and
        /// deletes the job.
        /// </summary>
        /// <param name="job">The incubation job</param>
        /// <param name="player">The player who receives the egg</param>
        void CreateBeastEgg(IncubationJob job, uint player);

        /// <summary>
        /// The weekday incubation checks for this player: the tester chosen day when set, otherwise the server day (UTC).
        /// The overrides are test-server shortcuts, kept in memory only.
        /// </summary>
        DayOfWeek GetIncubationServerDay(string playerId);

        /// <summary>Whether a tester chose a weekday for this player.</summary>
        bool HasIncubationDayOverride(string playerId);

        /// <summary>Makes incubation treat the given weekday as today for this player (test servers only).</summary>
        void SetIncubationDayOverride(string playerId, DayOfWeek day);

        /// <summary>Goes back to the server day for this player.</summary>
        void ClearIncubationDayOverride(string playerId);

        /// <summary>
        /// Creates a beast egg of a beast type on a player.
        /// </summary>
        /// <param name="beastType">The type of beast the egg holds</param>
        /// <param name="player">The player who receives the egg</param>
        /// <returns>The egg</returns>
        uint CreateBeastEgg(BeastType beastType, uint player);

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        void OnRemoveProperty();

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        void UseExtractDNAObject();
    }
}
