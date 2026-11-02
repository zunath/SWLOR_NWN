using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The values one Espionage recipe gives the Reaction minigame. Set by <c>RecipeBuilder.ReactionParameters</c>.
    /// </summary>
    public sealed class ReactionParameters : RecipeMinigameParameters
    {
        /// <summary>How the cycle's turns are shared between the phases.</summary>
        public ReactionCyclePattern CyclePattern { get; }

        /// <summary>The number of turns in one full cycle, 4 to 8.</summary>
        public int CycleLength { get; }

        /// <summary>How violent the Peak is, 1 to 5. The recipe's tier sets it.</summary>
        public int PeakIntensity { get; }

        public ReactionParameters(ReactionCyclePattern cyclePattern, int cycleLength, int peakIntensity)
        {
            CyclePattern = cyclePattern;
            CycleLength = cycleLength;
            PeakIntensity = peakIntensity;
        }

        public override IReadOnlyList<RecipeParameterValue> Values => new[]
        {
            new RecipeParameterValue("cyclePattern", (int)CyclePattern),
            new RecipeParameterValue("cycleLength", CycleLength),
            new RecipeParameterValue("peakIntensity", PeakIntensity)
        };
    }
}
