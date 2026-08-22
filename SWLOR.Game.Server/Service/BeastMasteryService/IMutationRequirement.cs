using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public interface IMutationRequirement
    {
        /// <summary>
        /// Evaluates this requirement against a specific incubation job.
        /// Returns an empty string when satisfied, otherwise a player-facing explanation
        /// of why it was not met.
        /// </summary>
        string CheckRequirements(IncubationJob job);

        /// <summary>
        /// Describes this requirement in the abstract, independent of any job.
        /// Consumed by incubation field notes so players can see what a mutation needs.
        /// </summary>
        string GetRequirementDescription();
    }
}
