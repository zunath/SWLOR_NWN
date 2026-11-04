using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// A known mutation recipe checked against an incubation job: one line per requirement and a short summary of what is missing.
    /// </summary>
    public readonly record struct MutationRecipeReport(IReadOnlyList<MutationRequirementLine> Lines, string Summary, bool IsReady);
}
